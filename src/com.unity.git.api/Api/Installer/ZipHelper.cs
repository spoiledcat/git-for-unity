using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace Unity.VersionControl.Git
{
	using ICSharpCode.SharpZipLib;
	using ICSharpCode.SharpZipLib.GZip;
	using ICSharpCode.SharpZipLib.Tar;
	using ICSharpCode.SharpZipLib.Zip;

	public interface IZipHelper
	{
		bool Extract(string archive, string outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress,
			Func<string, bool> onFilter = null);
	}

	public class ZipHelper : IZipHelper
	{
		private static IZipHelper instance;

		public static IZipHelper Instance
		{
			get
			{
				if (instance == null)
					instance = new ZipHelper();
				return instance;
			}
			set => instance = value;
		}

		public bool Extract(string archive, string outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{

			var destDir = outFolder.ToNPath();
			destDir.EnsureDirectoryExists();
			if (archive.EndsWith(".tar.gz") || archive.EndsWith(".tgz"))
			{
				var gzipFile = archive.ToNPath();
                var tempDir = NPath.CreateTempDirectory("unzip");
                string outFilename = gzipFile.FileNameWithoutExtension;
                if (archive.EndsWith(".tgz"))
                    outFilename += ".tar";
                archive = tempDir.Combine(outFilename);
				using (var instream = NPath.FileSystem.OpenRead(gzipFile))
				using (var outstream = NPath.FileSystem.OpenWrite(archive, FileMode.CreateNew))
				{
					GZip.Decompress(instream, outstream, false);
				}
			}

			if (archive.EndsWith(".zip"))
                return ExtractZip(archive, destDir, cancellationToken, onStart, onProgress, onFilter);
			return ExtractTar(archive, destDir, cancellationToken, onStart, onProgress, onFilter);
		}

		private bool ExtractZip(string archive, NPath outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{
			ZipFile zf = null;

			try
			{
				var fs = NPath.FileSystem.OpenRead(archive);
				zf = new ZipFile(fs);
				List<IArchiveEntry> entries = PreprocessEntries(outFolder, zf, onStart, onFilter);
				return ExtractArchive(archive, outFolder, cancellationToken, zf, entries, onStart, onProgress, onFilter);
			}
			catch (Exception ex)
			{
				LogHelper.GetLogger<ZipHelper>().Error(ex);
				throw;
			}
			finally
			{
				zf?.Close(); // Ensure we release resources
			}
		}

		private bool ExtractTar(string archive, NPath outFolder, CancellationToken cancellationToken,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{
			TarArchive zf = null;

			try
			{
				List<IArchiveEntry> entries;
				using (var read = TarArchive.CreateInputTarArchive(NPath.FileSystem.OpenRead(archive)))
				{
					entries = PreprocessEntries(outFolder, read, onStart, onFilter);
				}
				zf = TarArchive.CreateInputTarArchive(NPath.FileSystem.OpenRead(archive));
				return ExtractArchive(archive, outFolder, cancellationToken, zf, entries, onStart, onProgress, onFilter);
			}
			catch (Exception ex)
			{
				LogHelper.GetLogger<ZipHelper>().Error(ex);
				throw;
			}
			finally
			{
				zf?.Close(); // Ensure we release resources
			}
		}

		private static bool ExtractArchive(string archive, NPath outFolder, CancellationToken cancellationToken,
			IArchive zf, List<IArchiveEntry> entries,
			Action<string, long> onStart, Func<long, long, string, bool> onProgress, Func<string, bool> onFilter = null)
		{

			const int chunkSize = 4096; // 4K is optimum
			foreach (var e in entries)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var filename = e.Name;
				var entry = zf.FindEntry(filename) ?? e;
				var fullZipToPath = MaybeSetPermissions(outFolder, filename, entry.FileAttributes);
				var targetFile = new FileInfo(fullZipToPath);

				var stream = zf.GetInputStream(entry);
				using (var streamWriter = targetFile.OpenWrite())
				{
					if (!Utils.Copy(stream, streamWriter, entry.Size, chunkSize,
						progress: (totalRead, timeToFinish) => {
							return onProgress?.Invoke(totalRead, entry.Size, filename) ?? true;
						}))
						return false;
				}

				targetFile.LastWriteTime = entry.LastModifiedTime;
			}
			return true;
		}

		private static List<IArchiveEntry> PreprocessEntries(NPath outFolder, IArchive zf, Action<string, long> onStart, Func<string, bool> onFilter)
		{
			var entries = new List<IArchiveEntry>();

			foreach (IArchiveEntry entry in zf)
			{
				if (entry.IsLink ||
					entry.IsSymLink)
					continue;

				if (entry.IsDirectory)
				{
					outFolder.Combine(entry.Name).EnsureDirectoryExists();
					continue; // Ignore directories
				}
				if (!onFilter?.Invoke(entry.Name) ?? false)
					continue;

				entries.Add(entry);
				onStart(entry.Name, entry.Size);
			}

			return entries;
		}

		private static NPath MaybeSetPermissions(NPath destDir, string entryFileName, int mode)
		{
			var fullZipToPath = destDir.Combine(entryFileName);
			fullZipToPath.EnsureParentDirectoryExists();
			try
			{
				if (NPath.IsUnix)
				{
					if (mode == -2115174400)
					{
						int fd = Mono.Unix.Native.Syscall.open(fullZipToPath,
							Mono.Unix.Native.OpenFlags.O_CREAT |
							Mono.Unix.Native.OpenFlags.O_TRUNC,
							Mono.Unix.Native.FilePermissions.S_IRWXU |
							Mono.Unix.Native.FilePermissions.S_IRGRP |
							Mono.Unix.Native.FilePermissions.S_IXGRP |
							Mono.Unix.Native.FilePermissions.S_IROTH |
							Mono.Unix.Native.FilePermissions.S_IXOTH);
						Mono.Unix.Native.Syscall.close(fd);
					}
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error(ex, "Error setting file attributes in " + fullZipToPath);
			}

			return fullZipToPath;
		}
	}
}

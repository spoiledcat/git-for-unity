using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unity.VersionControl.Git.ICSharpCode.SharpZipLib
{
	public interface IArchiveEntry
	{
        string Name { get; }
        long Size { get; }
		bool IsDirectory { get; }
		int FileAttributes { get; }
		bool IsSymLink { get; }
		bool IsLink { get; }
        DateTime LastModifiedTime { get; }
	}

	public interface IArchive : IEnumerable
	{
		Stream GetInputStream(IArchiveEntry entry);
		IArchiveEntry FindEntry(string name);
	}
}

// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using Internal.IO;
	using Logging;
	using Unity.Editor.Tasks.Helpers;

	public class DownloadTask : TaskBase<string>
	{
		private readonly CancellationTokenSource cts;
		public DownloadTask(
			ITaskManager taskManager,
			UriString url,
			string targetDirectory,
			string filename = null,
			int retryCount = 0,
			CancellationToken token = default)
			: base(taskManager, token)
		{
			cts = CancellationTokenSource.CreateLinkedTokenSource(Token);

			RetryCount = retryCount;
			Url = url;
			Filename = string.IsNullOrEmpty(filename) ? url.Filename : filename;
			this.targetDirectory = targetDirectory.ToSPath();
			Name = $"Download {Url}";
			Message = Filename;
		}

		public override string ToString()
		{
			return $"{base.ToString()} {Url}";
		}

		protected string BaseRunWithReturn(bool success)
		{
			return base.RunWithReturn(success);
		}

		protected override string RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);
			try
			{
				result = RunDownload(success);
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
			return result;
		}

		/// <summary>
		/// The actual functionality to download with optional hash verification
		/// subclasses that wish to return the contents of the downloaded file
		/// or do something else with it can override this instead of RunWithReturn.
		/// </summary>
		/// <param name="success"></param>
		/// <returns></returns>
		protected virtual string RunDownload(bool success)
		{
			Exception exception = null;
			var attempts = 0;
			bool result = false;
			var partialFile = targetDirectory.Combine(Filename + ".partial");
			targetDirectory.EnsureDirectoryExists();
			do
			{
				exception = null;

				if (cts.IsCancellationRequested)
					break;

				try
				{
					Logger.Trace($"Download of {Url} to {Destination} Attempt {attempts + 1} of {RetryCount + 1}");

					using (var destinationStream = partialFile.OpenWrite(FileMode.Append))
					{
						result = Downloader.Download(Logger, Url, destinationStream,
							 (value, total) => {
								 UpdateProgress(value, total);
								 return !cts.IsCancellationRequested;
							 });
					}

					if (result)
					{
						partialFile.Move(Destination);
					}
				}
				catch (Exception ex)
				{
					exception = ex;
					result = false;
				}
			} while (!cts.IsCancellationRequested && !result && attempts++ < RetryCount);

			if (!result)
			{
				cts.Token.ThrowIfCancellationRequested();
				throw new DownloadException("Error downloading file", exception);
			}

			return Destination;
		}

		public UriString Url { get; }

		private SPath targetDirectory;
		public string TargetDirectory => targetDirectory.ToString();

		public string Filename { get; }

		public string Destination => targetDirectory.Combine(Filename).ToString();

		protected int RetryCount { get; }
	}

	public class DownloadException : Exception
	{
		public DownloadException(string message) : base(message)
		{ }

		public DownloadException(string message, Exception innerException) : base(message, innerException)
		{ }
	}

	public static class WebRequestExtensions
	{
		public static WebResponse GetResponseWithoutException(this WebRequest request)
		{
			try
			{
				return request.GetResponse();
			}
			catch (WebException e)
			{
				if (e.Response != null)
				{
					return e.Response;
				}

				throw;
			}
		}
	}

	public class DownloadData
	{
		public DownloadData(UriString url, string file)
		{
			Url = url;
			File = file;
		}

		public UriString Url { get; }
		public string File { get; }
	}

	public class Downloader : TaskQueue<string, DownloadData>
	{
		/// <summary>
		/// Called for every queued download task when it finishes.
		/// </summary>
		public event Action<UriString, string> OnDownloadComplete;
		/// <summary>
		/// Called for every queued download task when it fails.
		/// </summary>
		public event Action<UriString, Exception> OnDownloadFailed;
		/// <summary>
		/// Called for every queued download task when it starts.
		/// </summary>
		public event Action<UriString> OnDownloadStart;

		/// <summary>
		/// TaskQueue of DownloaderTask objects that can download multiple
		/// things in parallel.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		public Downloader(ITaskManager taskManager, CancellationToken token = default)
			 : base(taskManager, t => {
				 var dt = t as DownloadTask;
				 var destinationFile = Path.Combine(dt.TargetDirectory, dt.Url.Filename);
				 return new DownloadData(dt.Url, destinationFile);
			 }, token)
		{
			Name = "Downloader";
			Message = "Downloading...";
		}

		public static bool Download(ILogging logger,
			UriString url,
			Stream destinationStream,
			Func<long, long, bool> onProgress = null)
		{
			url.EnsureNotNull(nameof(url));
			destinationStream.EnsureNotNull(nameof(destinationStream));

			long bytes = destinationStream.Length;

			var expectingResume = bytes > 0;

#if !NET_35
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif
			var webRequest = (HttpWebRequest)WebRequest.Create(url.ToUri());

			if (expectingResume)
			{
#if NET_35
				// classlib for 3.5 doesn't take long overloads...
				webRequest.AddRange((int)bytes);
#else
				webRequest.AddRange(bytes);
#endif
			}

			webRequest.Method = "GET";
			webRequest.Accept = "*/*";
			webRequest.UserAgent = "gfu/2.0";
			webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.BypassCache);
			webRequest.ServicePoint.ConnectionLimit = 10;
			webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
			webRequest.AllowAutoRedirect = true;

			if (logger != null)
			{
				if (expectingResume)
					logger.Trace($"Resuming download of {url}");
				else
					logger.Trace($"Downloading {url}");
			}

			if (!(onProgress?.Invoke(bytes, bytes * 2) ?? true))
				return false;

			using (var webResponse = (HttpWebResponse)webRequest.GetResponseWithoutException())
			{
				var httpStatusCode = webResponse.StatusCode;
				logger?.Trace($"Downloading {url} StatusCode:{(int)webResponse.StatusCode}");

				if (expectingResume && httpStatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
				{
					return !onProgress?.Invoke(bytes, bytes) ?? false;
				}

				if (!(httpStatusCode == HttpStatusCode.OK || httpStatusCode == HttpStatusCode.PartialContent))
				{
					return false;
				}

				if (expectingResume && httpStatusCode == HttpStatusCode.OK)
				{
					expectingResume = false;
					destinationStream.Seek(0, SeekOrigin.Begin);
				}

				var responseLength = webResponse.ContentLength;
				responseLength = responseLength > 0 ? webResponse.ContentLength : 0;
				if (expectingResume)
				{
					if (!onProgress?.Invoke(bytes, bytes + responseLength) ?? true)
						return false;
				}

				using (var responseStream = webResponse.GetResponseStream())
				{
					return Utils.Copy(responseStream, destinationStream, responseLength,
						 progress: (totalRead, timeToFinish) => {
							 return onProgress?.Invoke(totalRead, responseLength) ?? true;
						 });
				}
			}
		}

		public void QueueDownload(UriString url, string targetDirectory, string filename = null, int retryCount = 0)
		{
			var download = new DownloadTask(TaskManager, url, targetDirectory, filename, retryCount, Token);
			download.OnStart += t => OnDownloadStart?.Invoke(((DownloadTask)t).Url);
			download.OnEnd += (t, res, s, ex) => {
				if (s)
					OnDownloadComplete?.Invoke(((DownloadTask)t).Url, res);
				else
					OnDownloadFailed?.Invoke(((DownloadTask)t).Url, ex);
			};
			// queue after hooking up events so OnDownload* gets called first
			Queue(download);
		}
	}
}

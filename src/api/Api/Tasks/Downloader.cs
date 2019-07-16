using System;
using System.IO;
using System.Net;
using System.Net.Cache;

namespace Unity.VersionControl.Git
{
    class DownloadData
    {
        public UriString Url { get; }
        public NPath File { get; }
        public DownloadData(UriString url, NPath file)
        {
            this.Url = url;
            this.File = file;
        }
    }

    class Downloader : TaskQueue<NPath, DownloadData>
    {
        public event Action<UriString> OnDownloadStart;
        public event Action<UriString, NPath> OnDownloadComplete;
        public event Action<UriString, Exception> OnDownloadFailed;

        private readonly IFileSystem fileSystem;
        public Downloader(IFileSystem fileSystem = null)
            : base(t =>
            {
                var dt = t as DownloadTask;
                var destinationFile = dt.TargetDirectory.Combine(dt.Url.Filename);
                return new DownloadData(dt.Url, destinationFile);
            })
        {
            this.fileSystem = fileSystem ?? NPath.FileSystem;
            Name = "Downloader";
            Message = "Downloading...";
        }

        public void QueueDownload(UriString url, NPath targetDirectory, string filename = null, int retryCount = 0)
        {
            var download = new DownloadTask(Token, fileSystem, url, targetDirectory, filename, retryCount);
            download.OnStart += t => OnDownloadStart?.Invoke(((DownloadTask)t).Url);
            download.OnEnd += (t, res, s, ex) =>
            {
                if (s)
                    OnDownloadComplete?.Invoke(((DownloadTask)t).Url, res);
                else
                    OnDownloadFailed?.Invoke(((DownloadTask)t).Url, ex);
            };
            // queue after hooking up events so OnDownload* gets called first
            Queue(download);
        }

        public static bool Download(ILogging logger, UriString url,
            Stream destinationStream,
            Func<long, long, bool> onProgress)
        {
            long bytes = destinationStream.Length;

            var expectingResume = bytes > 0;

#if !NET35
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
#endif

            var webRequest = (HttpWebRequest)WebRequest.Create(url);

            if (expectingResume)
            {
#if NET35
                // classlib for 3.5 doesn't take long overloads...
                webRequest.AddRange((int)bytes);
#else
				webRequest.AddRange(bytes);
#endif
            }

            webRequest.Method = "GET";
            webRequest.Accept = "*/*";
            webRequest.UserAgent = "git-for-unity/2.0";
            webRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.BypassCache);
            webRequest.ServicePoint.ConnectionLimit = 10;
            webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            webRequest.AllowAutoRedirect = true;
            webRequest.Timeout = ApplicationConfiguration.WebTimeout;

            if (expectingResume)
                logger.Trace($"Resuming download of {url}");
            else
                logger.Trace($"Downloading {url}");

            if (!onProgress(bytes, bytes * 2))
                return false;

            using (var webResponse = (HttpWebResponse)webRequest.GetResponseWithoutException())
            {
                var httpStatusCode = webResponse.StatusCode;
                logger.Trace($"Downloading {url} StatusCode:{(int)webResponse.StatusCode}");

                if (expectingResume && httpStatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    return !onProgress(bytes, bytes);
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
                if (expectingResume)
                {
                    if (!onProgress(bytes, bytes + responseLength))
                        return false;
                }

                using (var responseStream = webResponse.GetResponseStream())
                {
                    return Utils.Copy(responseStream, destinationStream, responseLength,
                        progress: (totalRead, timeToFinish) =>
                        {
                            return onProgress(totalRead, responseLength);
                        });
                }
            }
        }
    }
}

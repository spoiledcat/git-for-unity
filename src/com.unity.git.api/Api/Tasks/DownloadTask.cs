using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Unity.VersionControl.Git
{
    using IO;

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

    public class DownloadTask : TaskBase<SPath>
    {
        protected readonly IFileSystem fileSystem;

        public DownloadTask(CancellationToken token,
            IFileSystem fileSystem,
            UriString url,
            SPath targetDirectory,
            string filename = null,
            int retryCount = 0)
            : base(token)
        {
            this.fileSystem = fileSystem;
            RetryCount = retryCount;
            Url = url;
            Filename = string.IsNullOrEmpty(filename) ? url.Filename : filename;
            TargetDirectory = targetDirectory;
            this.Name = $"Downloading {Url}";
        }

        protected override SPath RunWithReturn(bool success)
        {
            var result = base.RunWithReturn(success);
            try
            {
                result = RunDownload(success);
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    ThrownException.Rethrow();
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
        protected virtual SPath RunDownload(bool success)
        {
            Exception exception = null;
            var attempts = 0;
            bool result = false;
            var partialFile = TargetDirectory.Combine(Filename + ".partial");
            TargetDirectory.EnsureDirectoryExists();
            do
            {
                exception = null;

                if (Token.IsCancellationRequested)
                    break;

                try
                {
                    Logger.Trace($"Download of {Url} to {Destination} Attempt {attempts + 1} of {RetryCount + 1}");
                    var progressMessage = $"Downloading {Filename}";
                    using (var destinationStream = fileSystem.OpenWrite(partialFile, FileMode.Append))
                    {
                        result = Downloader.Download(Logger, Url, destinationStream,
                            (value, total) =>
                            {
                                UpdateProgress(value, total, progressMessage);
                                return !Token.IsCancellationRequested;
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
            } while (!result && attempts++ < RetryCount);

            if (!result)
            {
                Token.ThrowIfCancellationRequested();
                throw new DownloadException($"Error downloading {Url}", exception);
            }

            return Destination;
        }

        public override string ToString()
        {
            return $"{base.ToString()} {Url}";
        }

        public UriString Url { get; }

        public SPath TargetDirectory { get; }

        public string Filename { get; }

        public SPath Destination { get { return TargetDirectory.Combine(Filename); } }

        protected int RetryCount { get; }
    }

    class DownloadException : Exception
    {
        public DownloadException(string message) : base(message)
        { }

        public DownloadException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}

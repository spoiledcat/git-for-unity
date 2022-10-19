using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    using IO;
	public class UnzipTask : TaskBase<SPath>
    {
        private readonly string archiveFilePath;
        private readonly SPath extractedPath;
        private readonly IZipHelper zipHelper;
        private readonly ProgressReporter progressReporter = new ProgressReporter();
        private readonly Dictionary<string, TaskData> tasks = new Dictionary<string, TaskData>();

        public UnzipTask(ITaskManager taskManager, SPath archiveFilePath, SPath extractedPath)
            : this(taskManager, archiveFilePath, extractedPath, null)
        {}

        public UnzipTask(ITaskManager taskManager, SPath archiveFilePath, SPath extractedPath,
            IZipHelper zipHelper)
            : base(taskManager)
        {
            this.archiveFilePath = archiveFilePath;
            this.extractedPath = extractedPath;
            this.zipHelper = zipHelper ?? ZipHelper.Instance;
            Name = $"Unzip {archiveFilePath.FileName}";
            Message = $"Extracting {System.IO.Path.GetFileName(archiveFilePath)}";
            progressReporter.OnProgress += p => {
                this.progress.UpdateProgress(p);
            };
        }

        protected SPath BaseRun(bool success)
        {
            return base.RunWithReturn(success);
        }

        protected override SPath RunWithReturn(bool success)
        {
            var ret = BaseRun(success);
            try
            {
                ret = RunUnzip(success);
            }
            catch (Exception ex)
            {
                if (!RaiseFaultHandlers(ex))
                    Exception.Rethrow();
            }
            return ret;
        }

        protected virtual SPath RunUnzip(bool success)
        {
            Logger.Trace("Unzip File: {0} to Path: {1}", archiveFilePath, extractedPath);

            Exception exception = null;
            var attempts = 0;
            do
            {
                if (Token.IsCancellationRequested)
                    break;

                exception = null;
                try
                {
                    success = zipHelper.Extract(archiveFilePath, extractedPath,
                        (file, size) => {
                            var task = new TaskData(file, size);
                            tasks.Add(file, task);
                            progressReporter.UpdateProgress(task.progress);
                        },
                        (fileRead, fileTotal, file) =>
                        {
                            if (tasks.TryGetValue(file, out TaskData task)) {
                                task.UpdateProgress(fileRead, fileTotal);
                                progressReporter.UpdateProgress(task.progress);
                                if (fileRead == fileTotal)
                                {
                                    tasks.Remove(file);
                                }
                            }
                            return !Token.IsCancellationRequested;
                        }, token: Token);

                    if (!success)
                    {
                        //extractedPath.DeleteIfExists();
                        var message = $"Failed to extract {archiveFilePath} to {extractedPath}";
                        exception = new UnzipException(message);
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                    success = false;
                }
            } while (attempts++ < RetryCount);

            if (!success)
            {
                Token.ThrowIfCancellationRequested();
                throw new UnzipException("Error unzipping file", exception);
            }
            return extractedPath;
        }
        protected int RetryCount { get; }
    }

    public class UnzipException : Exception {
        public UnzipException(string message) : base(message)
        { }

        public UnzipException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}

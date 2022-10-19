using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public interface IPlatform
    {
        IPlatform Initialize(SynchronizationContext synchronizationContext);
        IPlatform Initialize();
        IGitEnvironment Environment { get; }
        IGitProcessManager ProcessManager { get; }
        IProcessEnvironment GitProcessEnvironment { get; }
        IProcessEnvironment DefaultProcessEnvironment { get; }
        ITaskManager TaskManager { get; }
        IGitClient GitClient { get; }
    }

    public class Platform : IPlatform, IDisposable
    {
        public Platform(IGitEnvironment environment)
        {
            Environment = environment;
            TaskManager = new TaskManager();
            ProcessManager = new GitProcessManager(Environment);
            GitClient = new GitClient(this);
            Instance = this;
        }

        public IPlatform Initialize(SynchronizationContext synchronizationContext)
        {
            TaskManager.Initialize(synchronizationContext);
            return this;
        }

        public IPlatform Initialize()
        {
            TaskManager.Initialize();
            return this;
        }

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                disposed = true;
                ProcessManager?.Stop();
                TaskManager?.Dispose();
            }
        }

        public void Dispose()
        {

        }

        public static IPlatform Instance { get; private set; }
        public IGitEnvironment Environment { get; }
        public IGitProcessManager ProcessManager { get; }
        public IProcessEnvironment GitProcessEnvironment => ProcessManager.GitProcessEnvironment;
        public IProcessEnvironment DefaultProcessEnvironment => ProcessManager.DefaultProcessEnvironment;
        public ITaskManager TaskManager { get; }
        public IGitClient GitClient { get; }
    }
}

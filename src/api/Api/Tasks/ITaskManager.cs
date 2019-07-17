using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.VersionControl.Git
{
    public interface ITaskManager : IDisposable
    {
        TaskScheduler ConcurrentScheduler { get; }
        TaskScheduler ExclusiveScheduler { get; }
        TaskScheduler UIScheduler { get; set; }
        CancellationToken Token { get; }

        T Schedule<T>(T task) where T : ITask;
        Task Wait();
        ITask Run(Action action, string message = null);
        ITask RunInUI(Action action);
        event Action<IProgress> OnProgress;
        /// <summary>
        /// Call this from the main thread so task manager knows which thread and
        /// synchronization context should be used
        /// </summary>
        void Initialize();
    }
}

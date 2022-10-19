// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Extensions;
	using Logging;

	public interface ITaskManager : IDisposable
	{
		event Action<IProgress> OnProgress;

		T Schedule<T>(T task) where T : ITask;
		ITask Run(Action action, string message = null);
		ITask RunInUI(Action action, string message = null);

		/// <summary>
		/// Call this from the main thread so task manager knows which thread is the main thread
		/// It uses the current synchronization context to queue tasks to the main thread
		/// </summary>
		ITaskManager Initialize();

		/// <summary>
		/// Call this from a thread different from the the main thread. This will call
		/// synchronizationContext.Send() in order to set up the task manager on the
		/// thread of the synchronizationContext.
		/// </summary>
		ITaskManager Initialize(SynchronizationContext synchronizationContext);

		TaskScheduler GetScheduler(TaskAffinity affinity);
		TaskScheduler ConcurrentScheduler { get; }
		TaskScheduler ExclusiveScheduler { get; }
		TaskScheduler UIScheduler { get; }
		CancellationToken Token { get; }
		bool InUIThread { get; }
		int UIThread { get; }
	}

	public class TaskManager : ITaskManager
	{
		private readonly ILogging logger;
		private readonly ConcurrentExclusiveSchedulerPairCustom manager;

		private readonly ProgressReporter progressReporter = new ProgressReporter();
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		private TaskScheduler uiScheduler;
		private bool stopped = false;

		public event Action<IProgress> OnProgress
		{
			add => progressReporter.OnProgress += value;
			remove => progressReporter.OnProgress -= value;
		}

		public TaskManager()
		{
			manager = new ConcurrentExclusiveSchedulerPairCustom(cts.Token);
			logger = LogHelper.GetLogger<TaskManager>();
		}

		/// <summary>
		/// Run this on the thread you would like to use as the main thread
		/// </summary>
		/// <returns></returns>
		public ITaskManager Initialize()
		{
			SetUIThread();
			uiScheduler = new SynchronizationContextTaskScheduler(SynchronizationContext.Current);
			return this;
		}

		/// <summary>
		/// Run this on a thread different from the main thread represented by the
		/// synchronization context.
		/// </summary>
		/// <param name="synchronizationContext"></param>
		/// <returns></returns>
		public ITaskManager Initialize(SynchronizationContext synchronizationContext)
		{
			uiScheduler = synchronizationContext.FromSynchronizationContext();
			synchronizationContext.Send(_ => SetUIThread(), null);
			return this;
		}

		public void SetUIThread()
		{
			UIThread = Thread.CurrentThread.ManagedThreadId;
		}

		public TaskScheduler GetScheduler(TaskAffinity affinity)
		{
			switch (affinity)
			{
				case TaskAffinity.Exclusive:
					return ExclusiveScheduler;
				case TaskAffinity.UI:
					return UIScheduler;
				case TaskAffinity.Custom:
					return null;
				case TaskAffinity.None:
					return TaskScheduler.Default;
				case TaskAffinity.Concurrent:
				default:
					return ConcurrentScheduler;
			}
		}

		public ITask Run(Action action, string message = null)
		{
			return new ActionTask(this, action) { Message = message }.Start();
		}

		public ITask RunInUI(Action action, string message = null)
		{
			return new ActionTask(this, action) { Affinity = TaskAffinity.UI, Message = message }.Start();
		}

		public T Schedule<T>(T task)
			where T : ITask
		{
			Schedule((TaskBase)(object)task, GetScheduler(task.Affinity), true, task.Affinity.ToString());
			return task;
		}

		private void Schedule(TaskBase task, TaskScheduler scheduler, bool setupFaultHandler, string schedulerName)
		{
			if (task.Affinity == TaskAffinity.Custom)
				scheduler = task.TaskScheduler;

			if (setupFaultHandler)
			{
				// we run this exception handler in the long running scheduler so it doesn't get blocked
				// by any exclusive tasks that might be running
				task.Task.ContinueWith(tt => {
					Exception ex = tt.Exception.GetBaseException();
					while (ex.InnerException != null) ex = ex.InnerException;
					logger.Trace(ex, $"Exception on {schedulerName} thread: {tt.Id} {task.Name}");
				},
					cts.Token,
					TaskContinuationOptions.OnlyOnFaulted,
					GetScheduler(TaskAffinity.None)
				);
			}

			task.Progress(progressReporter.UpdateProgress);
			task.InternalStart(scheduler);
		}

		public void Stop()
		{
			if (stopped) return;
			stopped = true;
			Dispose();
		}

		private bool disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				disposed = true;
				try
				{
					// tell all schedulers to stop scheduling new tasks
					manager.Complete();
					(uiScheduler as SynchronizationContextTaskScheduler)?.Dispose();

					// tell all tasks to exit
					cts.Cancel();

					// wait for everything to shut down within 500ms
					manager.Completion.Wait(500);
				}
				catch { }
				cts.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public TaskScheduler UIScheduler => uiScheduler;
		public TaskScheduler ConcurrentScheduler => manager.ConcurrentScheduler;
		public TaskScheduler ExclusiveScheduler => manager.ExclusiveScheduler;
		public CancellationToken Token => cts.Token;
		public int UIThread { get; private set; }
		public bool InUIThread => UIThread == 0 || UIThread == Thread.CurrentThread.ManagedThreadId;
	}
}


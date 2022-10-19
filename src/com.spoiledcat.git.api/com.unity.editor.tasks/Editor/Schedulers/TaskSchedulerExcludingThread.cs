namespace Unity.Editor.Tasks
{
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using Helpers;

	public class TaskSchedulerExcludingThread : TaskScheduler
	{
		public TaskSchedulerExcludingThread(int threadToExclude)
		{
			ThreadToExclude = threadToExclude;
		}

		public void ExecuteTask(Task task)
		{
			TryExecuteTask(task);
		}

		protected override void QueueTask(Task task)
		{
			task.EnsureNotNull(nameof(task));

			if ((task.CreationOptions & TaskCreationOptions.LongRunning) != TaskCreationOptions.None)
				new Thread(LongRunningThreadWork) {
					IsBackground = true
				}.Start(new TaskData { Scheduler = this, Task = task });
			else
				ThreadPool.QueueUserWorkItem(LongRunningThreadWork, new TaskData { Scheduler = this, Task = task });
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			try
			{
				if (Thread.CurrentThread.ManagedThreadId == ThreadToExclude)
					return false;
				return TryExecuteTask(task);
			}
			finally
			{
				if (taskWasPreviouslyQueued)
					NotifyWorkItemProgress();
			}
		}

		protected override bool TryDequeue(Task task)
		{
			return false;
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			yield return (Task)null;
		}

		private void NotifyWorkItemProgress()
		{
		}

		private static void LongRunningThreadWork(object obj)
		{
			var taskData = (TaskData)obj;
			taskData.Scheduler.TryExecuteTask(taskData.Task);
		}

		public int ThreadToExclude { get; set; }

		public IEnumerable<Task> Tasks { get; } = new Queue<Task>();

		struct TaskData
		{
			public TaskSchedulerExcludingThread Scheduler;
			public Task Task;
		}
	}
}

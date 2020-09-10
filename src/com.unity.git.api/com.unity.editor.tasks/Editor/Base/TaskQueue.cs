namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Helpers;

	/// <summary>
	/// A class that can run multiple <see cref="ITask" /> tasks concurrently. This task succeeds if
	/// all queued tasks succeed. If one or more queued tasks fail, this task fails, and the exception
	/// it returns is an aggregate of all the exceptions of the failed tasks.
	/// </summary>
	public class TaskQueue : TPLTask
	{
		private readonly TaskCompletionSource<bool> aggregateTask = new TaskCompletionSource<bool>();
		private readonly List<ITask> queuedTasks = new List<ITask>();
		private int finishedTaskCount;

		/// <summary>
		/// Creates an instance of the TaskQueue.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		public TaskQueue(ITaskManager taskManager, CancellationToken token = default) : base(taskManager, token)
		{
			Initialize(aggregateTask.Task);
		}

		/// <summary>
		/// Adds a task to this TaskQueue.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public ITask Queue(ITask task)
		{
			task.EnsureNotNull(nameof(task));

			// if this task fails, both OnEnd and Catch will be called
			// if a task before this one on the chain fails, only Catch will be called
			// so avoid calling TaskFinished twice by ignoring failed OnEnd calls
			task.OnEnd += InvokeFinishOnlyOnSuccess;
			task.Catch(e => TaskFinished());
			queuedTasks.Add(task);
			return this;
		}

		/// <summary>
		/// Runs all the tasks and blocks until all tasks are finished. The tasks will
		/// run in their own threads according to their task affinity, so running this
		/// in a background thread won't deadlock.
		/// If this task queue's affinity is set for UI/Exclusive and any queued tasks are
		/// also on the same affinity, this will deadlock, so don't do that.
		/// </summary>
		public override void RunSynchronously()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(true);
			}

			base.RunSynchronously();
		}

		/// <inheritdoc />
		protected override void Schedule()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(true);
			}

			base.Schedule();
		}

		private void InvokeFinishOnlyOnSuccess(ITask task, bool success, Exception ex)
		{
			if (success)
				TaskFinished();
		}

		private void TaskFinished()
		{
			var count = Interlocked.Increment(ref finishedTaskCount);
			if (count == queuedTasks.Count)
			{
				var exceptions = queuedTasks.Where(x => !x.Successful).Select(x => x.Exception).ToArray();
				var isSuccessful = exceptions.Length == 0;

				if (isSuccessful)
				{
					aggregateTask.TrySetResult(true);
				}
				else
				{
					aggregateTask.TrySetException(new AggregateException(exceptions));
				}
			}
		}
	}

	/// <summary>
	/// A class that can run multiple <see cref="ITask" /> tasks concurrently, and returns an aggregate
	/// of all the tasks results as a <see cref="List&lt;TResult&gt;" />.
	/// The individual tasks that are queued to this TaskQueue must all return the same type <typeparam name="TTaskResult"/>,
	/// but the TaskQueue returns a list of <typeparam name="TResult" />. 
	/// To convert from <typeparamref name="TTaskResult"/> to <typeparamref name="TResult" />, pass in a converter in a constructor.
	/// This task succeeds if all queued tasks succeed.
	/// If one or more queued tasks fail, this task fails, and the exception it returns is an aggregate of all the exceptions of the failed tasks.
	/// </summary>
	public class TaskQueue<TTaskResult, TResult> : TPLTask<List<TResult>>
	{
		private readonly TaskCompletionSource<List<TResult>> aggregateTask = new TaskCompletionSource<List<TResult>>();
		private readonly ProgressReporter progressReporter = new ProgressReporter();
		private readonly List<ITask<TTaskResult>> queuedTasks = new List<ITask<TTaskResult>>();
		private readonly Func<ITask<TTaskResult>, TResult> resultConverter;
		private int finishedTaskCount;

		/// <summary>
		/// If <typeparamref name="TTaskResult"/> is not assignable to <typeparamref name="TResult"/>, you must pass a
		/// method to convert between the two. Implicit conversions don't count.
		/// </summary>
		/// <param name="token"></param>
		/// <param name="resultConverter"></param>
		/// <param name="taskManager"></param>
		public TaskQueue(ITaskManager taskManager, Func<ITask<TTaskResult>, TResult> resultConverter = null, CancellationToken token = default)
			: base(taskManager, token)
		{
			// this excludes implicit operators - that requires using reflection to figure out if
			// the types are convertible, and I'd rather not do that
			if (resultConverter == null && !typeof(TResult).IsAssignableFrom(typeof(TTaskResult)))
			{
				throw new ArgumentNullException(nameof(resultConverter),
					String.Format(CultureInfo.InvariantCulture, "Cannot cast {0} to {1} and no {2} method was passed in to do the conversion", typeof(TTaskResult), typeof(TResult), nameof(resultConverter)));
			}

			this.resultConverter = resultConverter;
			Initialize(aggregateTask.Task);
			progressReporter.OnProgress += progress.UpdateProgress;
		}

		/// <summary>
		/// Queues an ITask for running
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public ITask<TTaskResult> Queue(ITask<TTaskResult> task)
		{
			progressReporter.Message = Message;

			// if this task fails, both OnEnd and Catch will be called
			// if a task before this one on the chain fails, only Catch will be called
			// so avoid calling TaskFinished twice by ignoring failed OnEnd calls
			task.Progress(progressReporter.UpdateProgress);
			task.OnEnd += InvokeFinishOnlyOnSuccess;
			task.Catch(e => TaskFinished());
			queuedTasks.Add(task);
			return task;
		}

		/// <inheritdoc />
		public override List<TResult> RunSynchronously()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(new List<TResult>());
			}

			return base.RunSynchronously();
		}

		/// <inheritdoc />
		protected override void Schedule()
		{
			if (queuedTasks.Any())
			{
				foreach (var task in queuedTasks)
					task.Start();
			}
			else
			{
				aggregateTask.TrySetResult(new List<TResult>());
			}

			base.Schedule();
		}

		private void InvokeFinishOnlyOnSuccess(ITask<TTaskResult> task, TTaskResult result, bool success, Exception ex)
		{
			if (success)
				TaskFinished();
		}

		private void TaskFinished()
		{
			var count = Interlocked.Increment(ref finishedTaskCount);
			if (count == queuedTasks.Count)
			{
				var exceptions = queuedTasks.Where(x => !x.Successful).Select(x => x.Exception).ToArray();
				var isSuccessful = exceptions.Length == 0;

				if (isSuccessful)
				{
					List<TResult> results;
					if (resultConverter != null)
						results = queuedTasks.Select(x => resultConverter(x)).ToList();
					else
						results = queuedTasks.Select(x => (TResult)(object)x.Result).ToList();
					aggregateTask.TrySetResult(results);
				}
				else
				{
					aggregateTask.TrySetException(new AggregateException(exceptions));
				}
			}
		}
	}

	/// <summary>
	/// A class that can run multiple <see cref="ITask" /> tasks concurrently, and returns a
	/// <see cref="List&lt;TResult&gt;" /> with all the results of the individual tasks.
	/// The individual tasks that are queued to this TaskQueue must all return the same
	/// type.
	/// This task succeeds if all queued tasks succeed.
	/// If one or more queued tasks fail, this task fails, and the exception
	/// it returns is an aggregate of all the exceptions of the failed tasks.
	/// </summary>
	public class TaskQueue<TResult> : TaskQueue<TResult, TResult>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		public TaskQueue(ITaskManager taskManager, CancellationToken token = default) : base(taskManager, token: token) { }
	}
}

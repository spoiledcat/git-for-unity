namespace Unity.Editor.Tasks
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	public class TPLTask : TaskBase
	{
		private Func<Task> taskGetter;
		private Task task;

		public TPLTask(ITaskManager taskManager, Func<Task> theGetter, CancellationToken token = default)
			: this(taskManager, token)
		{
			Initialize(theGetter);
		}

		// main constructor, everyone should go through here
		protected TPLTask(ITaskManager taskManager, CancellationToken token = default)
			: base(taskManager, token)
		{
			Task = new Task(InternalRunSynchronously, Token, TaskCreationOptions.None);
		}

		/// <summary>
		/// Call this if you're subclassing and haven't called one of the main public constructors
		/// </summary>
		/// <param name="theGetter"></param>
		protected void Initialize(Func<Task> theGetter)
		{
			taskGetter = theGetter;
		}

		/// <summary>
		/// Call this if you're subclassing and haven't called one of the main public constructors
		/// </summary>
		/// <param name="theTask"></param>
		protected void Initialize(Task theTask)
		{
			task = theTask;
		}

		protected override void Run(bool success)
		{
			base.Run(success);

			Token.ThrowIfCancellationRequested();
			try
			{
				var scheduler = TaskManager.GetScheduler(TaskAffinity.None);
				if (taskGetter != null)
				{
					var innerTask = Task.Factory.StartNew(taskGetter, Token, TaskCreationOptions.None, scheduler);
					innerTask.Wait(Token);
					task = innerTask.Result;
				}

				if (task.Status == TaskStatus.Created && !task.IsCompleted &&
					((task.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
				{
					Token.ThrowIfCancellationRequested();
					task.RunSynchronously(scheduler);
				}
				else
					task.Wait(Token);
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
				Token.ThrowIfCancellationRequested();
			}
		}
	}

	public class TPLTask<T> : TaskBase<T>
	{
		private Func<Task<T>> taskGetter;
		private Task<T> task;

		public TPLTask(ITaskManager taskManager, Func<Task<T>> theGetter, CancellationToken token = default)
			: this(taskManager, token)
		{
			Initialize(theGetter);
		}

		// main constructor, everyone should go through here
		protected TPLTask(ITaskManager taskManager, CancellationToken token = default)
			: base(taskManager, token)
		{
			Task = new Task<T>(InternalRunSynchronously, Token, TaskCreationOptions.None);
		}

		/// <summary>
		/// Call this if you're subclassing and haven't called one of the main public constructors
		/// </summary>
		/// <param name="theGetter"></param>
		protected void Initialize(Func<Task<T>> theGetter)
		{
			taskGetter = theGetter;
		}

		/// <summary>
		/// Call this if you're subclassing and haven't called one of the main public constructors
		/// </summary>
		/// <param name="theTask"></param>
		protected void Initialize(Task<T> theTask)
		{
			task = theTask;
		}

		protected override T RunWithReturn(bool success)
		{
			var ret = base.RunWithReturn(success);

			Token.ThrowIfCancellationRequested();
			try
			{
				var scheduler = TaskManager.GetScheduler(TaskAffinity.None);
				if (taskGetter != null)
				{
					var innerTask = Task<Task<T>>.Factory.StartNew(taskGetter, Token, TaskCreationOptions.None, scheduler);
					innerTask.Wait(Token);
					task = innerTask.Result;
				}

				if (task.Status == TaskStatus.Created && !task.IsCompleted &&
					((task.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
				{
					Token.ThrowIfCancellationRequested();
					task.RunSynchronously(scheduler);
				}
				ret = task.Result;
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
				Token.ThrowIfCancellationRequested();
			}
			return ret;
		}
	}

	public class TPLTask<T, TResult> : TaskBase<T, TResult>
	{
		private Func<T, Task<TResult>> taskGetter;
		private Task<TResult> task;

		public TPLTask(ITaskManager taskManager, Func<T, Task<TResult>> theGetter, Func<T> getPreviousResult = null, CancellationToken token = default)
			: this(taskManager, getPreviousResult, token)
		{
			Initialize(theGetter);
		}

		// main constructor, everyone should go through here
		protected TPLTask(ITaskManager taskManager, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, getPreviousResult, token)
		{
			Task = new Task<TResult>(InternalRunSynchronously, Token, TaskCreationOptions.None);
		}

		/// <summary>
		/// Call this if you're subclassing and haven't called one of the main public constructors
		/// </summary>
		/// <param name="theGetter"></param>
		protected void Initialize(Func<T, Task<TResult>> theGetter)
		{
			taskGetter = theGetter;
		}

		/// <summary>
		/// Call this if you're subclassing and haven't called one of the main public constructors
		/// </summary>
		/// <param name="theTask"></param>
		protected void Initialize(Task<TResult> theTask)
		{
			task = theTask;
		}

		protected override TResult RunWithData(bool success, T previousResult)
		{
			var result = base.RunWithData(success, previousResult);

			Token.ThrowIfCancellationRequested();
			try
			{
				var scheduler = TaskManager.GetScheduler(TaskAffinity.None);
				if (taskGetter != null)
				{
					var innerTask = Task<Task<TResult>>.Factory.StartNew(s => taskGetter((T)s), previousResult, Token, TaskCreationOptions.None, scheduler);
					innerTask.Wait(Token);
					task = innerTask.Result;
				}

				if (task.Status == TaskStatus.Created && !task.IsCompleted &&
					((task.CreationOptions & (TaskCreationOptions)512) == TaskCreationOptions.None))
				{
					Token.ThrowIfCancellationRequested();
					task.RunSynchronously(scheduler);
				}
				result = task.Result;
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
				Token.ThrowIfCancellationRequested();
			}
			return result;
		}
	}
}

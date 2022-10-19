// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Logging;
	using Helpers;

	/// <summary>
	/// Sets where a task will run depending on the success/failure of the tasks it depends on.
	/// </summary>
	public enum TaskRunOptions
	{
        /// <summary>
		/// Only run when previous tasks did not fail.
		/// </summary>
		OnSuccess,
        /// <summary>
		/// Only run when previous tasks failed.
		/// </summary>
		OnFailure,
        /// <summary>
		/// Always run regardless of success/failure of previous tasks.
		/// </summary>
		OnAlways
	}

    /// <summary>
	/// A task.
	/// </summary>
	public interface ITask : IAsyncResult
	{
        /// <summary>
		/// Raised when the task is starting. This is called on the same thread as the task.
		/// </summary>
		event Action<ITask> OnStart;

        /// <summary>
		/// Raised when the task is finished, regardless of failure or success. This is called on the same thread as the task.
		/// </summary>
		event Action<ITask, bool, Exception> OnEnd;

        /// <summary>
		/// Append a task that will run when this task has finished running.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="continuation"></param>
		/// <param name="runOptions"></param>
		/// <param name="taskIsTopOfChain"></param>
		/// <returns></returns>
		T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false) where T : ITask;

        /// <summary>
		/// Handler called when a task fails.
		/// </summary>
		/// <param name="handler"></param>
		/// <returns></returns>
		ITask Catch(Action<Exception> handler);

        /// <summary>
		/// Handler called when a task fails. This will run on the same thread as the task.
		/// If this handler returns true, the task becomes successful. Any exceptions supressed by this handler
		/// will still be available in the task object.
		/// </summary>
		/// <param name="handler"></param>
		/// <returns>True to supress this failure.</returns>
		ITask Catch(Func<Exception, bool> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// </summary>
		ITask FinallyInline(Action<bool> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		ITask Finally(Action<bool, Exception> actionToContinueWith, string name = null, TaskAffinity affinity = TaskAffinity.None);

		/// <summary>
		/// Run another task at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		T Finally<T>(T taskToContinueWith) where T : ITask;

        /// <summary>
		/// Start a task.
		/// </summary>
		/// <returns></returns>
		ITask Start();

        /// <summary>
        /// Start a task.
        /// </summary>
        /// <returns></returns>
        ITask Start(TaskScheduler customScheduler);

        /// <summary>
		/// Executes the body of the task. This is called when tasks run, but if you want to run the task directly, you can call this.
		/// </summary>
		void RunSynchronously();

        /// <summary>
		/// Handler called with progress reporting for the task. This runs on the same thread as the task.
		/// </summary>
		/// <param name="progressHandler"></param>
		/// <returns></returns>
		ITask Progress(Action<IProgress> progressHandler);

        /// <summary>
		/// Method called by tasks to update their progress.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="total"></param>
		/// <param name="message"></param>
		void UpdateProgress(long value, long total, string message = null);

        /// <summary>
		/// Get the task at the beginning of a chain of tasks.
		/// </summary>
		/// <param name="onlyCreated"></param>
		/// <returns></returns>
		ITask GetTopOfChain(bool onlyCreated = true);

        /// <summary>
		/// Get the task at the end of a chain of tasks. This is a bit iffy, because tasks can be a tree with multiple endpoints.
		/// </summary>
		/// <returns></returns>
		ITask GetEndOfChain();

		/// <summary>Checks whether any task on the chain is marked as exclusive.</summary>
		/// <returns>true if any task on the chain is marked as exclusive</returns>
		bool IsChainExclusive();

        /// <summary>
		/// Was the task successful?
		/// </summary>
		bool Successful { get; }

        /// <summary>
		/// Any errors the task set.
		/// </summary>
		string Errors { get; }

        /// <summary>
		/// The TPL task object, useful for direct awaiting.
		/// </summary>
		Task Task { get; }

        /// <summary>
		/// The name of this task, useful for logging.
		/// </summary>
		string Name { get; }

        /// <summary>
		/// The task affinity.
		/// </summary>
		TaskAffinity Affinity { get; set; }

        /// <summary>
		/// The cancellation token this task is listening to.
		/// </summary>
		CancellationToken Token { get; }

        /// <summary>
		/// The task manager this task is attached to.
		/// </summary>
		ITaskManager TaskManager { get; }

        /// <summary>
		/// The task that this task depends on, if any.
		/// </summary>
		TaskBase DependsOn { get; }

        /// <summary>
		/// Error message.
		/// </summary>
		string Message { get; }

        /// <summary>
		/// Exceptions set by this task.
		/// </summary>
		Exception Exception { get; }
	}

    /// <summary>
	/// An ITask that returns a result.
	/// </summary>
	/// <typeparam name="TResult"></typeparam>
	public interface ITask<TResult> : ITask
	{
		/// <summary>
		/// Raised when the task is starting.  Runs on the same thread as the task.
		/// </summary>
		new event Action<ITask<TResult>> OnStart;
        /// <summary>
		/// Raised when the task is finished, regardless of failure. Runs on the same thread as the task.
		/// </summary>
		new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;

        /// <summary>
        /// Handler called when a task fails.
        /// </summary>

		new ITask<TResult> Catch(Action<Exception> handler);

        /// <summary>
        /// Handler called when a task fails. This will run on the same thread as the task.
        /// If this handler returns true, the task becomes successful. Any exceptions supressed by this handler
        /// will still be available in the task object.
        /// </summary>
		new ITask<TResult> Catch(Func<Exception, bool> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// </summary>
		ITask<TResult> FinallyInline(Action<bool, TResult> handler);

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.None);

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		ITask Finally(Action<bool, Exception, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.None);

        /// <summary>
		/// Starts a task.
		/// </summary>
		/// <returns></returns>
		new ITask<TResult> Start();

        /// <summary>
        /// Starts a task.
        /// </summary>
        /// <returns></returns>
        new ITask<TResult> Start(TaskScheduler customScheduler);

        /// <summary>
        /// Executes the body of the task. This is called when tasks run, but if you want to run the task directly, you can call this.
        /// </summary>
		new TResult RunSynchronously();

        /// <summary>
        /// Handler called with progress reporting for the task. This runs on the same thread as the task.
        /// </summary>
        /// <param name="progressHandler"></param>
        /// <returns></returns>
        new ITask<TResult> Progress(Action<IProgress> progressHandler);

        /// <summary>
		/// The result value of the task.
		/// </summary>
		TResult Result { get; }

        /// <summary>
		/// The underlying TPL task object, useful for awaiting.
		/// </summary>
		new Task<TResult> Task { get; }
	}

    /// <summary>
	/// An ITask that raises an event whenever it creates data.
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="T"></typeparam>
	public interface ITask<TData, T> : ITask<T>
	{
        /// <summary>
		/// Raised when the task creates data.
		/// </summary>
		event Action<TData> OnData;
	}

    /// <summary>
	/// A task.
	/// </summary>
    public class TaskBase : ITask
	{
        /// <summary>
		/// The TPL enums for running continuations that we rely on.
		/// </summary>
		protected const TaskContinuationOptions runAlwaysOptions = TaskContinuationOptions.None;
        /// <summary>
        /// The TPL enums for running continuations that we rely on.
        /// </summary>
		protected const TaskContinuationOptions runOnSuccessOptions = TaskContinuationOptions.OnlyOnRanToCompletion;
        /// <summary>
        /// The TPL enums for running continuations that we rely on.
        /// </summary>
		protected const TaskContinuationOptions runOnFaultOptions = TaskContinuationOptions.OnlyOnFaulted;

        /// <summary>
		/// A default empty completed task.
		/// </summary>
		public static ITask Default = new TaskBase { Name = "Global", Task = TaskHelpers.GetCompletedTask() };

        /// <summary>
		/// The continuation to schedule when this task is done.
		/// </summary>
		protected TaskBase continuationOnAlways;
        /// <summary>
		/// The continuation to schedule if this task fails.
		/// </summary>
		protected TaskBase continuationOnFailure;

        /// <summary>
		/// The continuation to schedule if this task succeeds.
		/// </summary>
		protected TaskBase continuationOnSuccess;

        /// <summary>
		/// If this task failed but a catch handler suppressed the failure, this will be true.
		/// </summary>
		protected bool exceptionWasHandled = false;
        /// <summary>
		/// If this task failed but a catch handler suppressed the failure, this will be true.
		/// </summary>
        protected bool taskFailed = false;

		/// <summary>
		/// Has this task run?
		/// </summary>
		protected bool hasRun = false;

		/// <summary>
		/// If the previous task failed, this will have that exception.
		/// </summary>
		protected Exception previousException;

        /// <summary>
		/// The previous task success value.
		/// </summary>
		protected bool? previousSuccess;

        /// <summary>
		/// This tasks's progress reporting object.
		/// </summary>
		protected Progress progress;

        /// <inheritdoc />
        public event Action<ITask> OnStart;
        /// <inheritdoc />
		public event Action<ITask, bool, Exception> OnEnd;

        /// <summary>
		/// The catch handler, if any.
		/// </summary>
		protected event Func<Exception, bool> catchHandler;

        private event Action<bool> finallyHandler;
		private ILogging logger;
		private Exception exception;
		private readonly CancellationTokenSource cts;

		/// <summary>
		/// Empty constructor for default instances.
		/// </summary>
		protected TaskBase() {}

        /// <summary>
		/// Creates an instance of TaskBase.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		protected TaskBase(ITaskManager taskManager, CancellationToken token = default)
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			cts = CancellationTokenSource.CreateLinkedTokenSource(taskManager.Token, token);
			TaskManager = taskManager;
			Token = cts.Token;
			progress = new Progress(this);
			Task = new Task(InternalRunSynchronously, Token, TaskCreationOptions.None);
		}

        /// <inheritdoc />
        public virtual T Then<T>(T nextTask, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
			where T : ITask
		{
			Guard.EnsureNotNull(nextTask, nameof(nextTask));
			var nextTaskBase = ((TaskBase)(object)nextTask);

			// find the task at the top of the chain
			if (!taskIsTopOfChain)
				nextTaskBase = nextTaskBase.GetTopMostTask() ?? nextTaskBase;
			// make the next task dependent on this one so it can get values from us
			nextTaskBase.SetDependsOn(this);
			var nextTaskFinallyHandler = nextTaskBase.finallyHandler;

			if (runOptions == TaskRunOptions.OnSuccess)
			{
				continuationOnSuccess = nextTaskBase;

				// if there are fault handlers in the chain we're appending, propagate them
				// up this chain as well
				if (nextTaskBase.continuationOnFailure != null)
					SetFaultHandler(nextTaskBase.continuationOnFailure);
				else if (nextTaskBase.continuationOnAlways != null)
					SetFaultHandler(nextTaskBase.continuationOnAlways);
				if (nextTaskBase.catchHandler != null)
					Catch(nextTaskBase.catchHandler);
				if (nextTaskFinallyHandler != null)
					FinallyInline(nextTaskFinallyHandler);
			}
			else if (runOptions == TaskRunOptions.OnFailure)
			{
				continuationOnFailure = nextTaskBase;
				DependsOn?.Then(nextTaskBase, TaskRunOptions.OnFailure, true);
			}
			else
			{
				continuationOnAlways = nextTaskBase;
				DependsOn?.SetFaultHandler(nextTaskBase);
			}

			// if the current task has a fault handler, attach it to the chain we're appending
			if (finallyHandler != null)
			{
				var endOfChainTask = (TaskBase)nextTaskBase.GetEndOfChain();
				while (endOfChainTask != this && endOfChainTask != null)
				{
					endOfChainTask.finallyHandler += finallyHandler;
					endOfChainTask = endOfChainTask.DependsOn;
				}
			}

			return nextTask;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same thread)
		/// Chain will be cancelled
		/// </summary>
		public ITask Catch(Action<Exception> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			catchHandler += e => { handler(e); return false; };
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same threaD)
		/// Return true if you want the task to completely successfully
		/// </summary>
		public ITask Catch(Func<Exception, bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			CatchInternal(handler);
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// This will always run on the same thread as the previous task
		/// </summary>
		public ITask FinallyInline(Action<bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			finallyHandler += handler;
			DependsOn?.FinallyInline(handler);
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public ITask Finally(Action<bool, Exception> actionToContinueWith, string name = null, TaskAffinity affinity = TaskAffinity.None)
		{
			Guard.EnsureNotNull(actionToContinueWith, nameof(actionToContinueWith));

			var finallyTask = new ActionTask(TaskManager, (s, ex) => {
				actionToContinueWith(s, ex);
				if (!s)
					ex.Rethrow();
			}) { Affinity = affinity, Name = name ?? (affinity == TaskAffinity.UI ? "FinallyInUI" : "Finally") };

			return Then(finallyTask, TaskRunOptions.OnAlways)
				.CatchInternal(_ => true);
		}

		/// <summary>
		/// Run another task at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public T Finally<T>(T taskToContinueWith)
			where T : ITask
		{
			return Then(taskToContinueWith, TaskRunOptions.OnAlways);
		}

		/// <summary>
		/// Progress provides progress reporting from the task (on the same thread)
		/// </summary>
		public ITask Progress(Action<IProgress> handler)
		{
			Guard.EnsureNotNull(handler, nameof(handler));
			progress.OnProgress += handler;
			return this;
		}

		/// <inheritdoc />
		public ITask Start()
		{
			var depends = GetTopMostStartableTask();
			depends?.Schedule();
			return this;
		}

		/// <inheritdoc />
		public ITask Start(TaskScheduler scheduler)
		{
			TaskScheduler = scheduler;
			var depends = GetTopMostStartableTask();
			if (depends != null)
			{
				depends.TaskScheduler = scheduler;
				depends.Schedule();
			}
			return this;
		}

		/// <inheritdoc />
		public virtual void RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess ?? (DependsOn?.Successful ?? true);
			try
			{
				Run(previousIsSuccessful);
			}
			finally
			{
				RaiseOnEnd();
			}
		}

		internal void InternalRunSynchronously()
		{
			current = this;
			RunSynchronously();
		}

		internal void InternalStart(TaskScheduler scheduler)
		{
			if (Task.Status == TaskStatus.Created)
			{
				if (scheduler == null)
				{
					var message = $"Missing scheduler on task {Name} with affinity {Affinity}.";
					if (Affinity == TaskAffinity.Custom)
					{
						message += " Tasks with custom affinity must be started with Start(TaskScheduler).";
					}
					else if (Affinity == TaskAffinity.UI)
					{
						message += " Did you call TaskManager.Initialize()?";
					}
					throw new InvalidOperationException(message);
				}
				Task.Start(scheduler);
			}
		}

		/// <inheritdoc />
		public ITask GetTopOfChain(bool onlyCreated = true)
		{
			return GetTopMostTask(null, onlyCreated, false);
		}

		/// <inheritdoc />
		public ITask GetEndOfChain()
		{
			if (continuationOnSuccess != null)
				return continuationOnSuccess.GetEndOfChain();
			else if (continuationOnAlways != null)
				return continuationOnAlways.GetEndOfChain();
			return this;
		}

		/// <summary>Checks whether any task on the chain is marked as exclusive.</summary>
		/// <returns>true if any task on the chain is marked as exclusive</returns>
		public bool IsChainExclusive()
		{
			if (Affinity == TaskAffinity.Exclusive)
				return true;
			return DependsOn?.IsChainExclusive() ?? false;
		}

		/// <inheritdoc />
		public void UpdateProgress(long value, long total, string message = null)
		{
			progress.UpdateProgress(value, total, message ?? Message);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()}";
		}

        /// <summary>
		/// Schedules this task on the task manager, if it hasn't been scheduled yet.
		/// </summary>
		protected virtual void Schedule()
		{
			if (Task.Status == TaskStatus.Created)
			{
				TaskManager.Schedule(this);
			}
		}

		/// <summary>
		/// Call this to run a task after another task is done, without
		/// having them depend on each other
		/// </summary>
		/// <param name="task"></param>
		protected void Start(Task task)
		{
			previousSuccess = task.Status == TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted;
			previousException = task.Exception;
			Task.Start(TaskManager.GetScheduler(Affinity));
			SetContinuation();
		}

		/// <summary>
		/// Set the continuationOnAlways continuation with runAlwaysOptions, if there's one.
		/// </summary>
		protected void SetContinuation()
		{
			if (continuationOnAlways != null)
			{
				SetContinuation(continuationOnAlways, runAlwaysOptions);
			}
		}

        /// <summary>
		/// Set a continuation to run one this task is done.
		/// </summary>
		/// <param name="continuation"></param>
		/// <param name="runOptions"></param>
		protected void SetContinuation(TaskBase continuation, TaskContinuationOptions runOptions)
		{
			Token.ThrowIfCancellationRequested();

			continuation.TaskScheduler = TaskScheduler;

			var scheduler = TaskManager.GetScheduler(continuation.Affinity);
			if (continuation.Affinity == TaskAffinity.Custom)
			{
				scheduler = TaskScheduler;
			}

			if (scheduler == null)
			{
				var message = $"Missing scheduler on task {Name} with affinity {Affinity}.";
				if (Affinity == TaskAffinity.Custom)
				{
					message += " Tasks with custom affinity must be started with Start(TaskScheduler).";
				}
				else if (Affinity == TaskAffinity.UI)
				{
					message += " Did you call TaskManager.Initialize()?";
				}
				throw new InvalidOperationException(message);
			}

			Task.ContinueWith(_ =>
				{
					Token.ThrowIfCancellationRequested();
					((TaskBase)(object)continuation).Schedule();
				},
				Token,
				runOptions,
				scheduler);
		}

        /// <summary>
		/// Set a task that this task depends on.
		/// </summary>
		/// <param name="dependsOn"></param>
		/// <returns></returns>
		protected ITask SetDependsOn(ITask dependsOn)
		{
			DependsOn = (TaskBase)dependsOn;
			return this;
		}

		/// <summary>
		/// Returns the first startable task on the chain. If the chain has been started
		/// already, returns null
		/// </summary>
		protected TaskBase GetTopMostStartableTask()
		{
			return GetTopMostTask(null, true, true);
		}

        /// <summary>
		/// Returns the first task in a created state on the chain.
		/// </summary>
		/// <returns></returns>
		protected TaskBase GetTopMostCreatedTask()
		{
			return GetTopMostTask(null, true, false);
		}

        /// <summary>
		/// Returns the top most task of the chain.
		/// </summary>
		/// <returns></returns>
		protected TaskBase GetTopMostTask()
		{
			return GetTopMostTask(null, false, false);
		}

        /// <summary>
		/// Returns the top most task of the chain according to the parameters.
		/// </summary>
		/// <param name="ret"></param>
		/// <param name="onlyCreated"></param>
		/// <param name="onlyUnstartedChain"></param>
		/// <returns></returns>
		protected TaskBase GetTopMostTask(TaskBase ret, bool onlyCreated, bool onlyUnstartedChain)
		{
			ret = (!onlyCreated || Task.Status == TaskStatus.Created ? this : ret);
			var depends = DependsOn;
			if (depends == null)
			{
				// if we're at the top of the chain and the chain has already been started
				// and we only care about unstarted chains, return null
				if (onlyUnstartedChain && Task.Status != TaskStatus.Created)
					return null;
				return ret;
			}
			return depends.GetTopMostTask(ret, onlyCreated, onlyUnstartedChain);
		}

        /// <summary>
        /// Sets state prior to executing the body of a task. This runs in thread and is overridden
        /// by subclasses to actually execute the body of the task. It should be called before anything else.
        /// </summary>
		/// <param name="success"></param>
		protected virtual void Run(bool success)
		{
			taskFailed = false;
			hasRun = false;
			Exception = null;
			Token.ThrowIfCancellationRequested();
		}

        /// <summary>
		/// Raises the OnStart event, setting the progress reporting object to 0.
		/// </summary>
		protected virtual void RaiseOnStart()
		{
			UpdateProgress(0, 100);
			RaiseOnStartInternal();
		}

        /// <summary>
		/// Raises the OnStart event.
		/// </summary>
		protected void RaiseOnStartInternal()
		{
			Logger.Debug($"OnStart: {Name} [{(TaskManager.InUIThread ? "UI Thread" : Affinity.ToString())}]");
			OnStart?.Invoke(this);
		}

        /// <summary>
		/// Calls catch handlers and sets exception and message properties. This should be called in a Run
		/// override when a task fails, before throwing the exception. If this returns true, the task should not throw.
		/// </summary>
		/// <param name="ex"></param>
		/// <returns></returns>
		protected virtual bool RaiseFaultHandlers(Exception ex)
		{
			Exception = ex;
			if (Exception is AggregateException)
				Exception = Exception.GetBaseException() ?? Exception;
			Errors = Exception.Message;
			taskFailed = true;
			if (catchHandler == null)
				return false;
			var args = new object[] { Exception };
			foreach (var handler in catchHandler.GetInvocationList())
			{
				if ((bool)handler.DynamicInvoke(args))
				{
					exceptionWasHandled = true;
					break;
				}
			}
			// if a catch handler returned true, don't throw
			return exceptionWasHandled;
		}

        /// <summary>
		/// Raises the OnEnd event, setting the progress reporting object to 100 and setting up continuations.
		/// </summary>
		protected virtual void RaiseOnEnd()
		{
			hasRun = true;
			RaiseOnEndInternal();
			SetupContinuations();
			UpdateProgress(100, 100);
		}

        /// <summary>
		/// Raises the OnEnd event.
		/// </summary>
		protected void RaiseOnEndInternal()
		{
			Logger.Trace($"OnEnd: {Name} [{(TaskManager.InUIThread ? "UI Thread" : Affinity.ToString())}]");
			OnEnd?.Invoke(this, !taskFailed, Exception);
		}

        /// <summary>
		/// Set up continuations when the task is done. Also calls in-thread finally handlers.
		/// </summary>
		protected void SetupContinuations()
		{
			if (!taskFailed || exceptionWasHandled)
			{
				var taskToContinueWith = continuationOnSuccess ?? continuationOnAlways;
				if (taskToContinueWith != null)
					SetContinuation(taskToContinueWith, runOnSuccessOptions);
				else
				{ // there are no more tasks to schedule, call a finally handler if it exists
				  // we need to do this only when there are no more continuations
				  // so that the in-thread finally handler is guaranteed to run after any Finally tasks
					CallFinallyHandler();
				}
			}
			else
			{
				var taskToContinueWith = continuationOnFailure ?? continuationOnAlways;
				if (taskToContinueWith != null)
					SetContinuation(taskToContinueWith, runOnFaultOptions);
				else
				{ // there are no more tasks to schedule, call a finally handler if it exists
				  // we need to do this only when there are no more continuations
				  // so that the in-thread finally handler is guaranteed to run after any Finally tasks
					CallFinallyHandler();
				}
			}
		}

        /// <summary>
		/// Calls any in-thread finally handlers.
		/// </summary>
		protected virtual void CallFinallyHandler()
		{
			finallyHandler?.Invoke(!taskFailed);
		}

        /// <summary>
		/// Gets the exception that was thrown by the task or any tasks before it.
		/// </summary>
		/// <returns></returns>
		protected Exception GetThrownException()
		{
			var depends = DependsOn;
			while (depends != null)
			{
				if (depends.taskFailed)
					return depends.Exception;
				depends = depends.DependsOn;
			}
			return previousException;
		}

		internal ITask CatchInternal(Func<Exception, bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			catchHandler += handler;
			return this;
		}

		/// <summary>
		/// This does not set a dependency between the two tasks. Instead,
		/// the Start method grabs the state of the previous task to pass on
		/// to the next task via previousSuccess and previousException
		/// </summary>
		/// <param name="handler"></param>
		internal void SetFaultHandler(TaskBase handler)
		{
			Task.ContinueWith(t =>
				{
					Token.ThrowIfCancellationRequested();
					//Logger.Trace($"SetFaultHandler: {handler.Name} Start()");
					handler.Start(t);
				},
				Token,
				TaskContinuationOptions.OnlyOnFaulted,
				TaskManager.GetScheduler(handler.Affinity));
			DependsOn?.SetFaultHandler(handler);
		}

		/// <inheritdoc />
		public Exception Exception
		{
			get => exception ?? (exception = GetThrownException());
			protected set => exception = value;
		}

		/// <inheritdoc />
		public virtual bool Successful => hasRun && !taskFailed;
		/// <inheritdoc />
		public bool IsCompleted => hasRun;
		/// <inheritdoc />
		public string Errors { get; protected set; }
		/// <inheritdoc />
		public Task Task { get; protected set; }
		/// <inheritdoc />
		public WaitHandle AsyncWaitHandle => (Task as IAsyncResult).AsyncWaitHandle;
		/// <inheritdoc />
		public object AsyncState => (Task as IAsyncResult).AsyncState;
		/// <inheritdoc />
		public bool CompletedSynchronously => (Task as IAsyncResult).CompletedSynchronously;
		/// <inheritdoc />
		public string Name { get; set; }
		/// <inheritdoc />
		public virtual TaskAffinity Affinity { get; set; }
		/// <inheritdoc />
		public TaskBase DependsOn { get; private set; }
		/// <inheritdoc />
		public CancellationToken Token { get; }
		/// <inheritdoc />
		public ITaskManager TaskManager { get; }
		/// <inheritdoc />
		public TaskScheduler TaskScheduler { get; private set; }
		/// <inheritdoc />
		public virtual string Message { get; set; }

		[ThreadStatic] protected static TaskBase current;
		public static ITask CurrentTask => current;

		/// <inheritdoc />
		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
	}

    /// <summary>
    /// A task returning a value.
    /// </summary>
	public class TaskBase<TResult> : TaskBase, ITask<TResult>
	{
		/// <summary>
		/// A default empty completed task.
		/// </summary>
		public new static ITask<TResult> Default = new TaskBase<TResult> { Name = "Global", Task = TaskHelpers.GetCompletedTask(default(TResult)) };

		public static ITask<TResult> FromResult(TResult result) => new TaskBase<TResult> { Name = "Global", Task = TaskHelpers.GetCompletedTask(result) };

		private TResult result;

		/// <inheritdoc />
		public new event Action<ITask<TResult>> OnStart;

		/// <inheritdoc />
		public new event Action<ITask<TResult>, TResult, bool, Exception> OnEnd;

		private event Action<bool, TResult> finallyHandler;

        /// <summary>
		/// Creates a TaskBase instance.
		/// </summary>
		protected TaskBase() {}

        /// <summary>
        /// Creates a TaskBase instance.
        /// </summary>
		protected TaskBase(ITaskManager taskManager, CancellationToken token = default)
			: base(taskManager, token)
		{
			Task = new Task<TResult>(InternalRunSynchronously, Token, TaskCreationOptions.None);
		}

        /// <inheritdoc />
		public override T Then<T>(T continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, bool taskIsTopOfChain = false)
		{
			var nextTask = base.Then<T>(continuation, runOptions, taskIsTopOfChain);
			var nextTaskBase = ((TaskBase)(object)nextTask);
			// if the current task has a fault handler that matches this signature, attach it to the chain we're appending
			if (finallyHandler != null)
			{
				var endOfChainTask = (TaskBase)nextTaskBase.GetEndOfChain();
				while (endOfChainTask != this && endOfChainTask != null)
				{
					if (endOfChainTask is TaskBase<TResult> taskBase)
						taskBase.finallyHandler += finallyHandler;
					endOfChainTask = endOfChainTask.DependsOn;
				}
			}
			return nextTask;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same threaD)
		/// Marks the catch as handled so other Catch statements down the chain
		/// won't be called for this exception (but the chain will be cancelled)
		/// </summary>
		public new ITask<TResult> Catch(Action<Exception> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			catchHandler += e => { handler(e); return false; };
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Catch runs right when the exception happens (on the same thread)
		/// Return false if you want other Catch statements on the chain to also
		/// get called for this exception
		/// </summary>
		public new ITask<TResult> Catch(Func<Exception, bool> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			CatchInternal(handler);
			DependsOn?.Catch(handler);
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on the same thread as the task that just finished, regardless of execution state
		/// This will always run on the same thread as the last task that runs
		/// </summary>
		public ITask<TResult> FinallyInline(Action<bool, TResult> handler)
		{
			Guard.EnsureNotNull(handler, "handler");
			finallyHandler += handler;
			DependsOn?.FinallyInline(success => handler(success, default(TResult)));
			return this;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public ITask<TResult> Finally(Func<bool, Exception, TResult, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.None)
		{
			Guard.EnsureNotNull(continuation, "continuation");

			var finallyTask = Then(new FuncTask<TResult, TResult>(TaskManager, continuation) { Affinity = affinity, Name = name ?? "Finally" }, TaskRunOptions.OnAlways);
			finallyTask.CatchInternal(_ => true);
			return finallyTask;
		}

		/// <summary>
		/// Run a callback at the end of the task execution, on a separate thread, regardless of execution state
		/// </summary>
		public ITask Finally(Action<bool, Exception, TResult> continuation, string name = null, TaskAffinity affinity = TaskAffinity.None)
		{
			Guard.EnsureNotNull(continuation, "continuation");

			var finallyTask = new ActionTask<TResult>(TaskManager, (s, ex, res) => {
				continuation(s, ex, res);
				if (!s)
					ex.Rethrow();
			}) { Affinity = affinity, Name = name ?? (affinity == TaskAffinity.UI ? "FinallyInUI" : "Finally") };

			return Then(finallyTask, TaskRunOptions.OnAlways)
				.CatchInternal(_ => true);
		}

		/// <inheritdoc />
		public new ITask<TResult> Start()
		{
			base.Start();
			return this;
		}

		/// <inheritdoc />
		public new ITask<TResult> Start(TaskScheduler customScheduler)
		{
			base.Start(customScheduler);
			return this;
		}

		/// <summary>
		/// Progress provides progress reporting from the task (on the same thread)
		/// </summary>
		public new ITask<TResult> Progress(Action<IProgress> handler)
		{
			base.Progress(handler);
			return this;
		}

		/// <inheritdoc />
		public new virtual TResult RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess ?? (DependsOn?.Successful ?? true);
			TResult ret = default;
			try
			{
				ret = RunWithReturn(previousIsSuccessful);
			}
			finally
			{
				RaiseOnEnd(ret);
			}
			return ret;
		}

		internal new TResult InternalRunSynchronously()
		{
			current = this;
			return RunSynchronously();
		}

		/// <summary>
		/// Empty implementation of the base <see cref="TaskBase.Run" /> method that
		/// returns the correct result type, so that implementations of this can follow
		/// the correct pattern (see example)
		/// </summary>
		/// <example><code lang="cs"><![CDATA[
		/// protected override TResult RunWithReturn(bool success)
		/// {
		///	    var result = base.RunWithReturn(success);
		///     try
		///     {
		///         if (Callback != null)
		///         {
		///             result = Callback(success);
		///         }
		///         else if (CallbackWithException != null)
		///         {
		///             var thrown = GetThrownException();
		///             result = CallbackWithException(success, thrown);
		///         }
		///     }
		///     catch (Exception ex)
		///     {
		///         if (!RaiseFaultHandlers(ex))
		///             Exception.Rethrow();
		///     }
		///     return result;
		/// }
		/// ]]></code></example>
		/// <param name="success"></param>
		/// <returns></returns>
		protected virtual TResult RunWithReturn(bool success)
		{
			base.Run(success);
			return result;
		}

		/// <inheritdoc />
		protected override void RaiseOnStart()
		{
			UpdateProgress(0, 100);
			OnStart?.Invoke(this);
			RaiseOnStartInternal();
		}

		/// <summary>
		/// Raises the OnEnd event, setting the progress reporting object to 100 and setting up continuations.
		/// </summary>
		protected virtual void RaiseOnEnd(TResult data)
		{
			result = data;
			hasRun = true;
			OnEnd?.Invoke(this, result, !taskFailed, Exception);
			RaiseOnEndInternal();
			UpdateProgress(100, 100);
			SetupContinuations();
		}

		/// <inheritdoc />
		protected override void CallFinallyHandler()
		{
			finallyHandler?.Invoke(!taskFailed, result);
			base.CallFinallyHandler();
		}

		/// <inheritdoc />
		public new Task<TResult> Task
		{
			get => base.Task as Task<TResult>;
			set => base.Task = value;
		}

		/// <inheritdoc />
		public TResult Result => result;
	}

	/// <summary>
	/// A task that wraps an <see cref="Func&lt;T, TResult&gt;" />-like method.
	/// The <typeparamref name="T"/> argument can be either the value of the previous task that this task depends on,
	/// or passed in via <see cref="getPreviousResult" />,
	/// or passed in via <see cref="PreviousResult" />
	/// </summary>
	/// <typeparam name="T">The type of the argument that the action is expecting</typeparam>
	/// <typeparam name="TResult">The type of the result the action returns.</typeparam>
	public abstract class TaskBase<T, TResult> : TaskBase<TResult>
	{
		private readonly Func<T> getPreviousResult;

		/// <summary>
		/// Creates an instance of TaskBase.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		protected TaskBase(ITaskManager taskManager, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, token)
		{
			Task = new Task<TResult>(RunSynchronously, Token, TaskCreationOptions.None);
			this.getPreviousResult = getPreviousResult;
		}

		/// <inheritdoc />
		public override TResult RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess ?? (DependsOn?.Successful ?? true);

			// if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
			// otherwise if there's a method to retrieve the value, call that
			// otherwise use the PreviousResult property
			T prevResult = PreviousResult;
			if (previousIsSuccessful && DependsOn != null && DependsOn is ITask<T>)
				prevResult = ((ITask<T>)DependsOn).Result;
			else if (getPreviousResult != null)
				prevResult = getPreviousResult();

			TResult ret = default;
			try
			{
				ret = RunWithData(previousIsSuccessful, prevResult);
			}
			finally
			{
				RaiseOnEnd(ret);
			}
			return ret;
		}

		/// <summary>
		/// Empty implementation of the base <see cref="TaskBase.Run" /> method that
		/// returns the correct result type, so that implementations of this can follow
		/// the correct pattern (see example)
		/// </summary>
		/// <example><code lang="cs"><![CDATA[
		/// protected override TResult RunWithData(bool success, T previousResult)
		/// {
		///	    var result = base.RunWithData(success, previousResult);
		///     try
		///     {
		///         if (Callback != null)
		///         {
		///             result = Callback(success, previousResult);
		///         }
		///         else if (CallbackWithException != null)
		///         {
		///             var thrown = GetThrownException();
		///             result = CallbackWithException(success, thrown, previousResult);
		///         }
		///     }
		///     catch (Exception ex)
		///     {
		///         if (!RaiseFaultHandlers(ex))
		///             Exception.Rethrow();
		///     }
		///     return result;
		/// }
		/// ]]></code></example>
		/// <param name="success"></param>
		/// <param name="previousResult"></param>
		/// <returns></returns>
		protected virtual TResult RunWithData(bool success, T previousResult)
		{
			base.Run(success);
			return default;
		}

		/// <inheritdoc />
		public T PreviousResult { get; set; } = default(T);
	}

    /// <summary>
	/// A Task that raises events when it produces data, and that returns data.
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	public abstract class DataTaskBase<TData, TResult> : TaskBase<TResult>, ITask<TData, TResult>
	{
		/// <inheritdoc />
		public event Action<TData> OnData;

        /// <summary>
		///
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		protected DataTaskBase(ITaskManager taskManager, CancellationToken token = default) : base(taskManager, token) {}

        /// <summary>
		/// Raises the OnData event.
		/// </summary>
		/// <param name="data"></param>
		protected void RaiseOnData(TData data)
		{
			OnData?.Invoke(data);
		}
	}

    /// <summary>
	/// A task that receives data, produces data, and returns data.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	public abstract class DataTaskBase<T, TData, TResult> : TaskBase<T, TResult>, ITask<TData, TResult>
	{
		/// <inheritdoc />
		public event Action<TData> OnData;

        /// <summary>
		///
		/// </summary>
		protected DataTaskBase(ITaskManager taskManager, Func<T> getPreviousResult = null, CancellationToken token = default) : base(taskManager, getPreviousResult, token) {}

		/// <summary>
		/// Raises the OnData event.
		/// </summary>
		/// <param name="data"></param>
		protected void RaiseOnData(TData data)
		{
			OnData?.Invoke(data);
		}
	}

	/// <summary>
	/// Stub task that can be used to update progress.
	/// </summary>
	public class TaskData : ITask
	{
		public Progress progress;

		event Action<ITask, bool, Exception> ITask.OnEnd
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}

		event Action<ITask> ITask.OnStart
		{
			add => throw new NotImplementedException();
			remove => throw new NotImplementedException();
		}

		public TaskData(string name, long total)
		{
			Message = name;
			Name = name;
			progress = new Progress(this);
			progress.Total = total;
		}

		public void UpdateProgress(long value, long total, string message = null)
		{
			progress.UpdateProgress(value, total, Name);
		}

		ITask ITask.Catch(Action<Exception> handler) => throw new NotImplementedException();

		ITask ITask.Catch(Func<Exception, bool> handler) => throw new NotImplementedException();

		ITask ITask.FinallyInline(Action<bool> handler) => throw new NotImplementedException();

		ITask ITask.Finally(Action<bool, Exception> actionToContinueWith, string name, TaskAffinity affinity) => throw new NotImplementedException();

		T ITask.Finally<T>(T taskToContinueWith) => throw new NotImplementedException();

		ITask ITask.GetEndOfChain() => throw new NotImplementedException();

		ITask ITask.GetTopOfChain(bool onlyCreated) => throw new NotImplementedException();

		bool ITask.IsChainExclusive() => false;

		ITask ITask.Progress(Action<IProgress> progressHandler) => throw new NotImplementedException();

		void ITask.RunSynchronously() => throw new NotImplementedException();

		ITask ITask.Start() => throw new NotImplementedException();
		ITask ITask.Start(TaskScheduler customScheduler) => throw new NotImplementedException();

		T ITask.Then<T>(T continuation, TaskRunOptions runOptions, bool taskIsTopOfChain) => throw new NotImplementedException();

		public string Message { get; set; }
		public string Name { get; set; }

		bool ITask.Successful => throw new NotImplementedException();

		string ITask.Errors => throw new NotImplementedException();

		Task ITask.Task => throw new NotImplementedException();

		TaskAffinity ITask.Affinity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		CancellationToken ITask.Token => throw new NotImplementedException();

		ITaskManager ITask.TaskManager => throw new NotImplementedException();

		TaskBase ITask.DependsOn => throw new NotImplementedException();

		Exception ITask.Exception => throw new NotImplementedException();

		object IAsyncResult.AsyncState => throw new NotImplementedException();

		WaitHandle IAsyncResult.AsyncWaitHandle => throw new NotImplementedException();

		bool IAsyncResult.CompletedSynchronously => throw new NotImplementedException();

		bool IAsyncResult.IsCompleted => throw new NotImplementedException();
	}

}

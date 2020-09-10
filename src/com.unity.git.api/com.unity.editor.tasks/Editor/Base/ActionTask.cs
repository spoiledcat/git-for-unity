// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Helpers;

	/// <summary>
	/// A task that wraps an <see cref="Action" />-like method.
	/// The action can take no arguments;
	/// take a <see cref="bool" /> argument, with the success/failure value of the task that it depends on;
	/// or take <see cref="bool" /> and <see cref="Exception" /> arguments, with the success/failure value of the task that it depends on
	/// and any thrown exceptions from it.
	/// </summary>
	public partial class ActionTask : TaskBase
	{
		/// <summary>
		/// Creates an instance of ActionTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = _ => action();
			Name = "ActionTask";
		}

		/// <summary>
		/// Creates an instance of ActionTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action<bool> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = action;
			Name = "ActionTask";
		}

		/// <summary>
		/// Creates an instance of ActionTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public ActionTask(ITaskManager taskManager, Action<bool, Exception> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			CallbackWithException = action;
			Name = "ActionTask<Exception>";
		}

		/// <inheritdoc />
		protected override void Run(bool success)
		{
			base.Run(success);
			try
			{
				Callback?.Invoke(success);
				if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					CallbackWithException?.Invoke(success, thrown);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
		}

		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Action<bool> Callback { get; }

		/// <summary>
		/// The delegate called to invoke the action, if the action cares about thrown
		/// exceptions.
		/// </summary>
		protected Action<bool, Exception> CallbackWithException { get; }
	}

	/// <summary>
	/// A task that wraps an <see cref="Action&lt;T&gt;" />-like method.
	/// The <typeparamref name="T"/> argument can be either the value of the previous task that this task depends on,
	/// or passed in via <see cref="getPreviousResult" />,
	/// or passed in via <see cref="PreviousResult" />
	/// The action can take one T argument;
	/// take a T and a <see cref="bool" /> argument, with the success/failure value of the task that it depends on;
	/// or take a T, a <see cref="bool" /> and <see cref="Exception" /> arguments, with the success/failure value of the task that it depends on
	/// and any thrown exceptions from it.
	/// </summary>
	/// <typeparam name="T">The type of the argument that the action is expecting</typeparam>
	public partial class ActionTask<T> : TaskBase
	{
		private readonly Func<T> getPreviousResult;

		/// <summary>
		/// Creates an instance of ActionTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, Action<bool, T> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");

			this.getPreviousResult = getPreviousResult;
			Callback = action;
			Task = new Task(InternalRunSynchronously, Token, TaskCreationOptions.None);
			Name = $"ActionTask<{typeof(T)}>";
		}

		/// <summary>
		/// Creates an instance of ActionTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with. You can also use the PreviousResult property to set this value</param>
		public ActionTask(ITaskManager taskManager, Action<bool, Exception, T> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");

			this.getPreviousResult = getPreviousResult;
			CallbackWithException = action;
			Task = new Task(InternalRunSynchronously, Token, TaskCreationOptions.None);
			Name = $"ActionTask<Exception, {typeof(T)}>";
		}

		/// <inheritdoc />
		public override void RunSynchronously()
		{
			RaiseOnStart();
			Token.ThrowIfCancellationRequested();
			var previousIsSuccessful = previousSuccess.HasValue ? previousSuccess.Value : (DependsOn?.Successful ?? true);

			// if this task depends on another task and the dependent task was successful, use the value of that other task as input to this task
			// otherwise if there's a method to retrieve the value, call that
			// otherwise use the PreviousResult property
			T prevResult = PreviousResult;
			if (previousIsSuccessful && DependsOn != null && DependsOn is ITask<T>)
				prevResult = ((ITask<T>)DependsOn).Result;
			else if (getPreviousResult != null)
				prevResult = getPreviousResult();

			try
			{
				Run(previousIsSuccessful, prevResult);
			}
			finally
			{
				RaiseOnEnd();
			}
		}

		/// <summary>
		/// Runs the action, raises fault handlers and sets exceptions.
		/// This is the main body of an ITask.
		/// If you want to override this, make sure to
		/// follow the implementation pattern in order to properly propagate exceptions.
		/// </summary>
		/// <param name="success">The success value of the task this depends on, if any</param>
		/// <param name="previousResult">The value returned from the task this depends on, if any, or
		/// whatever was passed in via PreviousResult/getPreviousResult</param>
		protected virtual void Run(bool success, T previousResult)
		{
			base.Run(success);
			try
			{
				Callback?.Invoke(success, previousResult);
				if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					CallbackWithException?.Invoke(success, thrown, previousResult);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
		}

		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Action<bool, T> Callback { get; }

		/// <summary>
		/// The delegate called to invoke the action, if the action cares about thrown
		/// exceptions.
		/// </summary>
		protected Action<bool, Exception, T> CallbackWithException { get; }

		/// <summary>
		/// The result of the task this one depends on, if any, or manually set by the caller, if needed.
		/// </summary>
		public T PreviousResult { get; set; } = default(T);
	}
}

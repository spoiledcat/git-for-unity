namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Helpers;

	/// <summary>
	/// A task that wraps a <see cref="Func&lt;T&gt;" />-like method.
	/// The action can take no arguments;
	/// take a <see cref="bool" /> argument, with the success/failure value of the task that it depends on;
	/// or take <see cref="bool" /> and <see cref="Exception" /> arguments, with the success/failure value of the task that it depends on
	/// and any thrown exceptions from it.
	/// </summary>
	/// <typeparam name="T">The type of the argument that the action returns</typeparam>
	public partial class FuncTask<T> : TaskBase<T>
	{
		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncTask(ITaskManager taskManager, Func<T> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = _ => action();
			Name = $"FuncTask<{typeof(T)}>";
		}

		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, T> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = action;
			Name = $"FuncTask<{typeof(T)}>";
		}

		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, Exception, T> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			CallbackWithException = action;
			Name = $"FuncTask<Exception, {typeof(T)}>";
		}

		/// <inheritdoc />
		protected override T RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);
			try
			{
				if (Callback != null)
				{
					result = Callback(success);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
			return result;
		}

		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Func<bool, T> Callback { get; }
		/// <summary>
		/// The delegate called to invoke the action, if the action cares about thrown
		/// exceptions.
		/// </summary>
		protected Func<bool, Exception, T> CallbackWithException { get; }
	}

	/// <summary>
	/// A task that wraps an <see cref="Func&lt;T, TResult&gt;" />-like method.
	/// The <typeparamref name="T"/> argument can be either the value of the previous task that this task depends on,
	/// or passed in via <see cref="TaskBase&lt;T, TResult&gt;.getPreviousResult" />,
	/// or passed in via <see cref="TaskBase&lt;T, TResult&gt;.PreviousResult" />
	/// The action can take one T argument;
	/// take a T and a <see cref="bool" /> argument, with the success/failure value of the task that it depends on;
	/// or take a T, a <see cref="bool" /> and <see cref="Exception" /> arguments, with the success/failure value of the task that it depends on
	/// and any thrown exceptions from it.
	/// </summary>
	/// <typeparam name="T">The type of the argument that the action is expecting</typeparam>
	/// <typeparam name="TResult"></typeparam>
	public partial class FuncTask<T, TResult> : TaskBase<T, TResult>
	{
		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, T, TResult> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, getPreviousResult, token: token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = action;
			Name = $"FuncTask<{typeof(T)}, {typeof(TResult)}>";
		}

		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		/// <param name="getPreviousResult"></param>
		public FuncTask(ITaskManager taskManager, Func<bool, Exception, T, TResult> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, getPreviousResult, token: token)
		{
			Guard.EnsureNotNull(action, "action");
			CallbackWithException = action;
			Name = $"FuncTask<{typeof(T)}, Exception, {typeof(TResult)}>";
		}

		/// <inheritdoc />
		protected override TResult RunWithData(bool success, T previousResult)
		{
			var result = base.RunWithData(success, previousResult);
			try
			{
				if (Callback != null)
				{
					result = Callback(success, previousResult);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown, previousResult);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
			return result;
		}

		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Func<bool, T, TResult> Callback { get; }
		/// <summary>
		/// The delegate called to invoke the action, if the action cares about thrown
		/// exceptions.
		/// </summary>
		protected Func<bool, Exception, T, TResult> CallbackWithException { get; }
	}

	/// <summary>
	/// A task that wraps an Func&lt;T, List&lt;T&gt;&gt;-like method.
	/// </summary>
	/// <typeparam name="T">The type of the argument that the action is expecting and returns in a list.</typeparam>
	public partial class FuncListTask<T> : DataTaskBase<T, List<T>>
	{
		/// <summary>
		/// Creates an instance of FuncListTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, List<T>> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = action;
		}

		/// <summary>
		/// Creates an instance of FuncListTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, Exception, List<T>> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			Guard.EnsureNotNull(action, "action");
			CallbackWithException = action;
		}

		/// <summary>
		/// Creates an instance of FuncListTask.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="action"></param>
		public FuncListTask(ITaskManager taskManager, Func<bool, FuncListTask<T>, List<T>> action, CancellationToken token = default)
			: base(taskManager, token)
		{
			action.EnsureNotNull("action");
			CallbackWithSelf = action;
		}

		/// <inheritdoc />
		protected override List<T> RunWithReturn(bool success)
		{
			var result = base.RunWithReturn(success);
			try
			{
				if (Callback != null)
				{
					result = Callback(success);
				}
				else if (CallbackWithSelf != null)
				{
					result = CallbackWithSelf(success, this);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
			finally
			{
				if (result == null)
					result = new List<T>();
			}
			return result;
		}

		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Func<bool, List<T>> Callback { get; }
		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Func<bool, FuncListTask<T>, List<T>> CallbackWithSelf { get; }
		/// <summary>
		/// The delegate called to invoke the action, if the action cares about thrown
		/// exceptions.
		/// </summary>
		protected Func<bool, Exception, List<T>> CallbackWithException { get; }
	}

	/// <summary>
	/// A task that wraps an Func&lt;T, List&lt;TResult&gt;&gt;-like method.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="TResult"></typeparam>
	public partial class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
	{
		/// <summary>
		/// Creates an instance of FuncListTask.
		/// </summary>
		public FuncListTask(ITaskManager taskManager, Func<bool, T, List<TResult>> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, getPreviousResult, token)
		{
			Guard.EnsureNotNull(action, "action");
			Callback = action;
		}

		/// <summary>
		/// Creates an instance of FuncListTask.
		/// </summary>
		public FuncListTask(ITaskManager taskManager, Func<bool, Exception, T, List<TResult>> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: base(taskManager, getPreviousResult, token)
		{
			Guard.EnsureNotNull(action, "action");
			CallbackWithException = action;
		}

		/// <inheritdoc />
		protected override List<TResult> RunWithData(bool success, T previousResult)
		{
			var result = base.RunWithData(success, previousResult);
			try
			{
				if (Callback != null)
				{
					result = Callback(success, previousResult);
				}
				else if (CallbackWithException != null)
				{
					var thrown = GetThrownException();
					result = CallbackWithException(success, thrown, previousResult);
				}
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
			return result;
		}

		/// <summary>
		/// The delegate called to invoke the action, in case the action doesn't care
		/// about thrown exceptions.
		/// </summary>
		protected Func<bool, T, List<TResult>> Callback { get; }
		/// <summary>
		/// The delegate called to invoke the action, if the action cares about thrown
		/// exceptions.
		/// </summary>
		protected Func<bool, Exception, T, List<TResult>> CallbackWithException { get; }
	}
}

namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	public partial class FuncTask<T, TResult> : TaskBase<T, TResult>
	{
		/// <summary>
		/// Creates an instance of FuncTask.
		/// </summary>
		public FuncTask(ITaskManager taskManager, Func<T, TResult> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: this(taskManager, (_, t) => action(t), getPreviousResult, token)
		{}
	}

	public partial class FuncListTask<T> : DataTaskBase<T, List<T>>
	{
		/// <summary>
		/// 
		/// </summary>
		public FuncListTask(ITaskManager taskManager, Func<List<T>> action, CancellationToken token = default)
			: this(taskManager, (_) => action(), token)
		{ }
	}

	public partial class FuncListTask<T, TData, TResult> : DataTaskBase<T, TData, List<TResult>>
	{
		/// <summary>
		/// 
		/// </summary>
		public FuncListTask(ITaskManager taskManager, Func<T, List<TResult>> action, Func<T> getPreviousResult = null, CancellationToken token = default)
			: this(taskManager, (_, t) => action(t), getPreviousResult, token)
		{ }
	}
}

// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace Unity.Editor.Tasks
{
	using Helpers;
	using System.Threading;

	public partial class ActionTask
	{
	}

	public partial class ActionTask<T> : TaskBase
	{
		/// <summary>
		/// Creates an instance of ActionTask<typeparamref name="T"/>.
		/// The delegate of the task will get the value that is returned from a previous task, if any, or from the <see cref="PreviousResult"/> property,
		/// if set.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action">A delegate thar receives a value of type T</param>
		/// <param name="token"></param>
		public ActionTask(ITaskManager taskManager, Action<T> action, CancellationToken token = default)
			: this(taskManager, (_, t) => action(t), token: token)
		{ }

		/// <summary>
		/// Creates an instance of ActionTask<typeparamref name="T"/>.
		/// The delegate of the task will get the value returned from the <paramref name="getPreviousResult"/> delegate.
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="action">A delegate thar receives a value of type T</param>
		/// <param name="getPreviousResult">Method to call that returns the value that this task is going to work with.</param>
		/// <param name="token"></param>
		public ActionTask(ITaskManager taskManager, Action<T> action, Func<T> getPreviousResult, CancellationToken token = default)
			: this(taskManager, (_, t) => action(t), getPreviousResult, token: token)
		{ }
	}
}

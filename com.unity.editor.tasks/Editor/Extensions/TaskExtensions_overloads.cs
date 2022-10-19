// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	public static partial class TaskExtensions
	{
		public static ITask Then(this ITask task, Action continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask Then(this ITask task, Action<bool, Exception> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask Then<T>(this ITask<T> task, Action<T> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask Then<T>(this ITask<T> task, Action<bool, Exception, T> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask<T> Then<T>(this ITask task, Func<T> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask<T> Then<T>(this ITask task, Func<bool, Exception, T> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<T, TRet> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<bool, Exception, T, TRet> continuation, string name) => Then(task, continuation, TaskAffinity.Concurrent, name);


		public static ITask ThenInUI(this ITask task, Action continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = "ThenInUI")
			 => Then(task, continuation, TaskAffinity.UI, name, runOptions);

		public static ITask ThenInUI<T>(this ITask<T> task, Action<T> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			 => Then(task, continuation, TaskAffinity.UI, name ?? $"ThenInUI<{typeof(T)}>", runOptions);

		public static ITask<T> ThenInUI<T>(this ITask task, Func<T> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> Then(task, continuation, TaskAffinity.UI, name ?? $"ThenInUIFunc<{typeof(T)}>", runOptions);

		public static ITask<TRet> ThenInUI<T, TRet>(this ITask<T> task, Func<T, TRet> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> Then(task, continuation, TaskAffinity.UI, name ?? $"ThenInUIFunc<{typeof(T)}, {typeof(TRet)}>", runOptions);


		public static ITask ThenInUI(this ITask task, Action continuation, string name) => ThenInUI(task, continuation, TaskRunOptions.OnSuccess, name);
		public static ITask ThenInUI<T>(this ITask<T> task, Action<T> continuation, string name) => ThenInUI(task, continuation, TaskRunOptions.OnSuccess, name);
		public static ITask<T> ThenInUI<T>(this ITask task, Func<T> continuation, string name) => ThenInUI(task, continuation, TaskRunOptions.OnSuccess, name);
		public static ITask<TRet> ThenInUI<T, TRet>(this ITask<T> task, Func<T, TRet> continuation, string name) => ThenInUI(task, continuation, TaskRunOptions.OnSuccess, name);

		public static ITask ThenInExclusive(this ITask task, Action continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = "ThenInExclusive")
			 => Then(task, continuation, TaskAffinity.Exclusive, name, runOptions);

		public static ITask ThenInExclusive<T>(this ITask<T> task, Action<T> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			 => Then(task, continuation, TaskAffinity.Exclusive, name ?? $"ThenInExclusive<{typeof(T)}>", runOptions);

		public static ITask<T> ThenInExclusive<T>(this ITask task, Func<T> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> Then(task, continuation, TaskAffinity.Exclusive, name ?? $"ThenInExclusiveFunc<{typeof(T)}>", runOptions);

		public static ITask<TRet> ThenInExclusive<T, TRet>(this ITask<T> task, Func<T, TRet> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> Then(task, continuation, TaskAffinity.Exclusive, name ?? $"ThenInExclusiveFunc<{typeof(T)}, {typeof(TRet)}>", runOptions);

		public static ITask ThenInExclusive(this ITask task, Action continuation, string name) => ThenInExclusive(task, continuation, TaskRunOptions.OnSuccess, name);
		public static ITask ThenInExclusive<T>(this ITask<T> task, Action<T> continuation, string name) => ThenInExclusive(task, continuation, TaskRunOptions.OnSuccess, name);
		public static ITask<T> ThenInExclusive<T>(this ITask task, Func<T> continuation, string name) => ThenInExclusive(task, continuation, TaskRunOptions.OnSuccess, name);
		public static ITask<TRet> ThenInExclusive<T, TRet>(this ITask<T> task, Func<T, TRet> continuation, string name) => ThenInExclusive(task, continuation, TaskRunOptions.OnSuccess, name);


		/**
		 * TPL Task section
		**/


		public static ITask ThenAsync(this ITask task, Func<Task> continuation, string name) => ThenAsync(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask<T> ThenAsync<T>(this ITask task, Func<Task<T>> continuation, string name) => ThenAsync(task, continuation, TaskAffinity.Concurrent, name);
		public static ITask<TRet> ThenAsync<T, TRet>(this ITask<T> task, Func<T, Task<TRet>> continuation, string name) => ThenAsync(task, continuation, TaskAffinity.Concurrent, name);

		public static ITask ThenInUIAsync(this ITask task, Func<Task> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = "ThenInUIAsync")
			=> ThenAsync(task, continuation, TaskAffinity.UI, name, runOptions);

		public static ITask ThenInUIAsync(this ITask task, Func<Task> continuation, string name)
			=> ThenInUIAsync(task, continuation, TaskRunOptions.OnSuccess, name);

		public static ITask<T> ThenInUIAsync<T>(this ITask task, Func<Task<T>> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> ThenAsync(task, continuation, TaskAffinity.UI, name ?? $"{nameof(ThenInUIAsync)}<{typeof(T)}>", runOptions);
		public static ITask<T> ThenInUIAsync<T>(this ITask task, Func<Task<T>> continuation, string name)
			=> ThenInUIAsync(task, continuation, TaskRunOptions.OnSuccess, name);

		public static ITask<TRet> ThenInUIAsync<T, TRet>(this ITask<T> task, Func<T, Task<TRet>> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> ThenAsync(task, continuation, TaskAffinity.UI, name ?? $"{nameof(ThenInUIAsync)}<{typeof(T)}>", runOptions);
		public static ITask<TRet> ThenInUIAsync<T, TRet>(this ITask<T> task, Func<T, Task<TRet>> continuation, string name)
			=> ThenInUIAsync(task, continuation, TaskRunOptions.OnSuccess, name);

		public static ITask ThenInExclusiveAsync(this ITask task, Func<Task> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = "ThenInExclusiveAsync")
			=> ThenAsync(task, continuation, TaskAffinity.Exclusive, name, runOptions);
		public static ITask ThenInExclusiveAsync(this ITask task, Func<Task> continuation, string name)
			=> ThenInExclusiveAsync(task, continuation, TaskRunOptions.OnSuccess, name);

		public static ITask<T> ThenInExclusiveAsync<T>(this ITask task, Func<Task<T>> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> ThenAsync(task, continuation, TaskAffinity.Exclusive, name ?? $"{nameof(ThenInExclusiveAsync)}<{typeof(T)}>", runOptions);
		public static ITask<T> ThenInExclusiveAsync<T>(this ITask task, Func<Task<T>> continuation, string name)
			=> ThenInExclusiveAsync(task, continuation, TaskRunOptions.OnSuccess, name);

		public static ITask<TRet> ThenInExclusiveAsync<T, TRet>(this ITask<T> task, Func<T, Task<TRet>> continuation, TaskRunOptions runOptions = TaskRunOptions.OnSuccess, string name = null)
			=> ThenAsync(task, continuation, TaskAffinity.Exclusive, name ?? $"{nameof(ThenInExclusiveAsync)}<{typeof(T)}>", runOptions);
		public static ITask<TRet> ThenInExclusiveAsync<T, TRet>(this ITask<T> task, Func<T, Task<TRet>> continuation, string name)
			=> ThenInExclusiveAsync(task, continuation, TaskRunOptions.OnSuccess, name);


		/**
		 * With section
		**/

		public static ITask With(this ITaskManager taskManager, Action continuation, string name) => With(taskManager, continuation, TaskAffinity.Concurrent, name);

		public static ITask With<T>(this ITaskManager taskManager, Action<T> continuation, T state, string name) => With(taskManager, continuation, state, TaskAffinity.Concurrent, name);

		public static ITask<T> With<T>(this ITaskManager taskManager, Func<T> continuation, string name) => With(taskManager, continuation, TaskAffinity.Concurrent, name);

		public static ITask<TRet> With<T, TRet>(this ITaskManager taskManager, Func<T, TRet> continuation, T state, string name) => With(taskManager, continuation, state, TaskAffinity.Concurrent, name);


		public static ITask WithUI(this ITaskManager taskManager, Action continuation, string name = "WithUI") => With(taskManager, continuation, TaskAffinity.UI, name);

		public static ITask WithUI<T>(this ITaskManager taskManager, Action<T> continuation, T state, string name = "WithUI") => With(taskManager, continuation, state, TaskAffinity.UI, name);

		public static ITask<T> WithUI<T>(this ITaskManager taskManager, Func<T> continuation, string name = "WithUI") => With(taskManager, continuation, TaskAffinity.UI, name);

		public static ITask<TRet> WithUI<T, TRet>(this ITaskManager taskManager, Func<T, TRet> continuation, T state, string name = "WithUI") => With(taskManager, continuation, state, TaskAffinity.UI, name);



		public static ITask WithExclusive(this ITaskManager taskManager, Action continuation, string name = "WithExclusive") => With(taskManager, continuation, TaskAffinity.Exclusive, name);

		public static ITask WithExclusive<T>(this ITaskManager taskManager, Action<T> continuation, T state, string name = "WithExclusive") => With(taskManager, continuation, state, TaskAffinity.Exclusive, name);

		public static ITask<T> WithExclusive<T>(this ITaskManager taskManager, Func<T> continuation, string name = "WithExclusive") => With(taskManager, continuation, TaskAffinity.Exclusive, name);

		public static ITask<TRet> WithExclusive<T, TRet>(this ITaskManager taskManager, Func<T, TRet> continuation, T state, string name = "WithExclusive") => With(taskManager, continuation, state, TaskAffinity.Exclusive, name);

		/**
		 * TPL Task section
		**/

		public static ITask WithAsync(this ITaskManager taskManager, Func<Task> asyncDelegate, string name) => WithAsync(taskManager, asyncDelegate, TaskAffinity.Concurrent, name);
		public static ITask<T> WithAsync<T>(this ITaskManager taskManager, Func<Task<T>> asyncDelegate, string name) => WithAsync(taskManager, asyncDelegate, TaskAffinity.Concurrent, name);
		public static ITask<TRet> WithAsync<T, TRet>(this ITaskManager taskManager, Func<T, Task<TRet>> asyncDelegate, T state, string name) => WithAsync(taskManager, asyncDelegate, state, TaskAffinity.Concurrent, name);

		public static ITask WithUIAsync(this ITaskManager taskManager, Func<Task> asyncDelegate, string name = "WithUIAsync") => WithAsync(taskManager, asyncDelegate, TaskAffinity.UI, name);
		public static ITask<TRet> WithUIAsync<TRet>(this ITaskManager taskManager, Func<Task<TRet>> asyncDelegate, string name = "WithUIAsync") => WithAsync(taskManager, asyncDelegate, TaskAffinity.UI, name);
		public static ITask<TRet> WithUIAsync<T, TRet>(this ITaskManager taskManager, Func<T, Task<TRet>> asyncDelegate, T state, string name = "WithUIAsync") => WithAsync(taskManager, asyncDelegate, state, TaskAffinity.UI, name);

		public static ITask WithExclusiveAsync(this ITaskManager taskManager, Func<Task> asyncDelegate, string name = "WithExclusiveAsync") => WithAsync(taskManager, asyncDelegate, TaskAffinity.Exclusive, name);
		public static ITask<T> WithExclusiveAsync<T>(this ITaskManager taskManager, Func<Task<T>> asyncDelegate, string name = "WithExclusiveAsync") => WithAsync(taskManager, asyncDelegate, TaskAffinity.Exclusive, name);
		public static ITask<TRet> WithExclusiveAsync<T, TRet>(this ITaskManager taskManager, Func<T, Task<TRet>> asyncDelegate, T state, string name = "WithExclusiveAsync") => WithAsync(taskManager, asyncDelegate, state, TaskAffinity.Exclusive, name);
	}
}

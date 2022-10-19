// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using System.Threading;
	using Helpers;

	public static partial class TaskExtensions
	{
		public static ITask With(this ITaskManager taskManager, Action continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = "With")
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			continuation.EnsureNotNull(nameof(continuation));
			return new ActionTask(taskManager, continuation) { Affinity = affinity, Name = name };
		}

		public static ITask With<T>(this ITaskManager taskManager, Action<T> continuation, T state, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null)
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			continuation.EnsureNotNull(nameof(continuation));
			return new ActionTask<T>(taskManager, continuation) { PreviousResult = state, Affinity = affinity, Name = name ?? $"With<{typeof(T)}>" };
		}

		public static ITask<T> With<T>(this ITaskManager taskManager, Func<T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null)
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			continuation.EnsureNotNull(nameof(continuation));
			return new FuncTask<T>(taskManager, continuation) { Affinity = affinity, Name = name ?? $"WithFunc<{typeof(T)}>" };
		}

		public static ITask<TRet> With<T, TRet>(this ITaskManager taskManager, Func<T, TRet> continuation, T state, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null)
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			continuation.EnsureNotNull(nameof(continuation));
			return new FuncTask<T, TRet>(taskManager, continuation) { PreviousResult = state, Affinity = affinity, Name = name ?? $"With<{typeof(T)}>" };
		}

		public static ITask WithAsync(this ITaskManager taskManager, Func<Task> asyncDelegate, TaskAffinity affinity = TaskAffinity.Concurrent, string name = "WithAsync")
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			asyncDelegate.EnsureNotNull(nameof(asyncDelegate));
			return new TPLTask(taskManager, asyncDelegate) { Affinity = affinity, Name = name };
		}

		public static ITask<TRet> WithAsync<TRet>(this ITaskManager taskManager, Func<Task<TRet>> asyncDelegate, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null)
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			asyncDelegate.EnsureNotNull(nameof(asyncDelegate));
			return new TPLTask<TRet>(taskManager, asyncDelegate) { Affinity = affinity, Name = name ?? $"WithAsync<{typeof(TRet)}>" };
		}

		public static ITask<TRet> WithAsync<T, TRet>(this ITaskManager taskManager, Func<T, Task<TRet>> asyncDelegate, T state, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null)
		{
			taskManager.EnsureNotNull(nameof(taskManager));
			asyncDelegate.EnsureNotNull(nameof(asyncDelegate));
			return new TPLTask<T, TRet>(taskManager, asyncDelegate) { PreviousResult = state, Affinity = affinity, Name = name ?? $"WithAsync<{typeof(T)}, {typeof(TRet)}>" };
		}

		public static ITask Then(this ITask task, Action continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = "Then", TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name }, runOptions);
		}

		public static ITask Then(this ITask task, Action<bool, Exception> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = "Then", TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name }, runOptions);
		}

		public static ITask Then<T>(this ITask<T> task, Action<T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask<T>(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name ?? $"Then<{typeof(T)}>" }, runOptions);
		}

		public static ITask Then<T>(this ITask<T> task, Action<bool, Exception, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new ActionTask<T>(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name ?? $"Then<{typeof(T)}>" }, runOptions);
		}

		public static ITask<T> Then<T>(this ITask task, Func<T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new FuncTask<T>(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name ?? $"ThenFunc<{typeof(T)}>" }, runOptions);
		}

		public static ITask<T> Then<T>(this ITask task, Func<bool, Exception, T> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new FuncTask<T>(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name ?? $"ThenFunc<{typeof(T)}>" }, runOptions);
		}

		public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<T, TRet> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new FuncTask<T, TRet>(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name ?? $"ThenFunc<{typeof(T)}, {typeof(TRet)}>" }, runOptions);
		}

		public static ITask<TRet> Then<T, TRet>(this ITask<T> task, Func<bool, Exception, T, TRet> continuation, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));
			return task.Then(new FuncTask<T, TRet>(task.TaskManager, continuation, token: task.Token) { Affinity = affinity, Name = name ?? $"ThenFunc<{typeof(T)}, {typeof(TRet)}>" }, runOptions);
		}

		public static ITask ThenAsync(this ITask task, Func<Task> asyncDelegate, TaskAffinity affinity = TaskAffinity.Concurrent, string name = "ThenAsync", TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			asyncDelegate.EnsureNotNull(nameof(asyncDelegate));
			var cont = new TPLTask(task.TaskManager, asyncDelegate, token: task.Token) { Affinity = affinity, Name = name};
			return task.Then(cont, runOptions);
		}

		public static ITask<T> ThenAsync<T>(this ITask task, Func<Task<T>> asyncDelegate, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			asyncDelegate.EnsureNotNull(nameof(asyncDelegate));
			var cont = new TPLTask<T>(task.TaskManager, asyncDelegate, token: task.Token) { Affinity = affinity, Name = name ?? $"ThenAsync<{typeof(T)}>" };
			return task.Then(cont, runOptions);
		}

		public static ITask<TRet> ThenAsync<T, TRet>(this ITask<T> task, Func<T, Task<TRet>> asyncDelegate, TaskAffinity affinity = TaskAffinity.Concurrent, string name = null, TaskRunOptions runOptions = TaskRunOptions.OnSuccess)
		{
			task.EnsureNotNull(nameof(task));
			asyncDelegate.EnsureNotNull(nameof(asyncDelegate));
			var cont = new TPLTask<T, TRet>(task.TaskManager, asyncDelegate, token: task.Token) { Affinity = affinity, Name = name ?? $"ThenAsync<{typeof(T)}, {typeof(TRet)}>" };
			return task.Then(cont, runOptions);
		}

		/// <summary>
		/// A finally handler that will be called in the UI thread. Finally handlers are always called when any task fails in a chain. If there are multiple finally handlers,
		/// their call order is not deterministic.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="task"></param>
		/// <param name="continuation"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static ITask FinallyInUI<T>(this T task, Action<bool, Exception> continuation, string name = null)
			where T : ITask
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));

			return task.Finally(continuation, name, TaskAffinity.UI);
		}

		/// <summary>
		/// A finally handler that will be called in the UI thread. Finally handlers are always called when any task fails in a chain. If there are multiple finally handlers,
		/// their call order is not deterministic.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="task"></param>
		/// <param name="continuation"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static ITask FinallyInUI<T>(this ITask<T> task, Action<bool, Exception, T> continuation, string name = null)
		{
			task.EnsureNotNull(nameof(task));
			continuation.EnsureNotNull(nameof(continuation));

			return task.Finally(continuation, name, TaskAffinity.UI);
		}

		/// <summary>
		/// Helper that starts an <see cref="ITask"/> and returns a <see cref="Task"/>, capturing exceptions and results. If you
		/// want to await an <see cref="ITask"/>, use this method.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="task"></param>
		/// <returns></returns>
		public static Task<T> StartAsAsync<T>(this ITask<T> task)
		{
			task.EnsureNotNull(nameof(task));

			var tcs = new TaskCompletionSource<T>();
			task.FinallyInline((success, r) => {
				tcs.TrySetResult(r);
			});
			task.Catch(e => {
				if (e is AggregateException)
					e = e.GetBaseException() ?? e;
				tcs.TrySetException(e);
			});
			task.Start();
			return tcs.Task;
		}

		/// <summary>
		/// Helper that starts an <see cref="ITask"/> and returns a <see cref="Task"/>, capturing exceptions. If you
		/// want to await an <see cref="ITask"/>, use this method.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public static Task<bool> StartAsAsync(this ITask task)
		{
			task.EnsureNotNull(nameof(task));

			var tcs = new TaskCompletionSource<bool>();
			task.FinallyInline(success => {
				tcs.TrySetResult(success);
			});
			task.Catch(e => {
				if (e is AggregateException)
					e = e.GetBaseException() ?? e;
				tcs.TrySetException(e);
			});
			task.Start();
			return tcs.Task;
		}

		public static async Task StartAwait(this ITask source, Action<Exception> handler = null)
		{
			try
			{
				await source.StartAsAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (handler == null)
					throw;
				handler(ex);
			}
		}

		public static async Task<T> StartAwait<T>(this ITask<T> source, Func<Exception, T> handler = null)
		{
			try
			{
				return await source.StartAsAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (handler == null)
					throw;
				return handler(ex);
			}
		}

		/// <summary>
		/// Starts a task, blocks until it's done or the token is cancelled, and returns the result;
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		public static T StartSync<T>(this ITask<T> source, CancellationToken token = default)
		{
			StartAwait(source).Wait(token);
			return source.Result;
		}
	}

	namespace Extensions
	{
		public static class TPLTaskExtensions
		{
			public static async Task<T> Timeout<T>(this Task<T> task, int timeout, string message, CancellationToken token = default)
			{
				var ret = await Task.WhenAny(task, Task.Delay(timeout, token));
				if (ret != task)
					throw new TimeoutException(message);
				return await task;
			}

			public static void FireAndForget(this Task _)
			{ }
		}
	}
}

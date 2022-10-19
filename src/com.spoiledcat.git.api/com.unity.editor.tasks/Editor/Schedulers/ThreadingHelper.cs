// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using System;
	using System.Runtime.CompilerServices;
	using Extensions;
	using Helpers;

	public static class ThreadingHelper
	{
		public static TaskScheduler GetUIScheduler(SynchronizationContext synchronizationContext) => synchronizationContext.FromSynchronizationContext();

		internal class AwaitableWrapper : IAwaitable
		{
			Func<IAwaiter> getAwaiter;

			public AwaitableWrapper()
			{
				getAwaiter = () => new AwaiterWrapper();
			}

			public AwaitableWrapper(TaskScheduler scheduler)
			{
				getAwaiter = () => new TaskSchedulerAwaiter(scheduler);
			}

			public AwaitableWrapper(IAwaiter awaiter)
			{
				getAwaiter = () => awaiter;
			}

			public IAwaiter GetAwaiter() => getAwaiter();
		}

		class AwaiterWrapper : IAwaiter
		{
			Func<bool> isCompleted;
			Action<Action> onCompleted;
			Action getResult;

			public AwaiterWrapper()
			{
				isCompleted = () => true;
				onCompleted = c => c();
				getResult = () => { };
			}

			public bool IsCompleted => isCompleted();

			public void OnCompleted(Action continuation) => onCompleted(continuation);

			public void GetResult() => getResult();
		}

		/// <summary>
		/// An awaitable that executes continuations on the specified task scheduler.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		readonly struct TaskSchedulerAwaitable
		{
			/// <summary>
			/// The scheduler for continuations.
			/// </summary>
			private readonly TaskScheduler taskScheduler;

			/// <summary>
			/// Initializes a new instance of the <see cref="TaskSchedulerAwaitable"/> struct.
			/// </summary>
			/// <param name="taskScheduler">The task scheduler used to execute continuations.</param>
			public TaskSchedulerAwaitable(TaskScheduler taskScheduler)
			{
				Guard.EnsureNotNull(taskScheduler, nameof(taskScheduler));

				this.taskScheduler = taskScheduler;
			}

			/// <summary>
			/// Gets an awaitable that schedules continuations on the specified scheduler.
			/// </summary>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
			public TaskSchedulerAwaiter GetAwaiter()
			{
				return new TaskSchedulerAwaiter(taskScheduler);
			}
		}

		/// <summary>
		/// An awaiter returned from GetAwaiter(TaskScheduler />.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		internal readonly struct TaskSchedulerAwaiter : ICriticalNotifyCompletion, IAwaiter
		{
			/// <summary>
			/// The scheduler for continuations.
			/// </summary>
			private readonly TaskScheduler scheduler;

			/// <summary>
			/// Initializes a new instance of the <see cref="TaskSchedulerAwaiter"/> struct.
			/// </summary>
			/// <param name="scheduler">The scheduler for continuations.</param>
			public TaskSchedulerAwaiter(TaskScheduler scheduler)
			{
				this.scheduler = scheduler;
			}

			/// <summary>
			/// Gets a value indicating whether no yield is necessary.
			/// </summary>
			/// <value><c>true</c> if the caller is already running on that TaskScheduler.</value>
			public bool IsCompleted
			{
				get
				{
					// We special case the TaskScheduler.Default since that is semantically equivalent to being
					// on a ThreadPool thread, and there are various ways to get on those threads.
					// TaskScheduler.Current is never null.  Even if no scheduler is really active and the current
					// thread is not a threadpool thread, TaskScheduler.Current == TaskScheduler.Default, so we have
					// to protect against that case too.
					bool isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
					return (scheduler == TaskScheduler.Default && isThreadPoolThread)
						|| (scheduler == TaskScheduler.Current && TaskScheduler.Current != TaskScheduler.Default);
				}
			}

			/// <summary>
			/// Schedules a continuation to execute using the specified task scheduler.
			/// </summary>
			/// <param name="continuation">The delegate to invoke.</param>
			public void OnCompleted(Action continuation)
			{
				if (scheduler == TaskScheduler.Default)
				{
					ThreadPool.QueueUserWorkItem(state => ((Action)state)(), continuation);
				}
				else
				{
					Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, scheduler);
				}
			}

			/// <summary>
			/// Schedules a continuation to execute using the specified task scheduler
			/// without capturing the ExecutionContext.
			/// </summary>
			/// <param name="continuation">The action.</param>
			public void UnsafeOnCompleted(Action continuation)
			{

				if (scheduler == TaskScheduler.Default)
				{
					ThreadPool.UnsafeQueueUserWorkItem(state => ((Action)state)(), continuation);
				}
				else
				{
					// There is no API for scheduling a Task without capturing the ExecutionContext.
					Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, scheduler);
				}
			}

			/// <summary>
			/// Does nothing.
			/// </summary>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
			public void GetResult()
			{
			}
		}

		/// <summary>
		/// A Task awaitable that has affinity to executing callbacks synchronously on the completing callstack.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		readonly struct ExecuteContinuationSynchronouslyAwaitable
		{
			/// <summary>
			/// The task whose completion will execute the continuation.
			/// </summary>
			private readonly Task antecedent;

			/// <summary>
			/// Initializes a new instance of the <see cref="ExecuteContinuationSynchronouslyAwaitable"/> struct.
			/// </summary>
			/// <param name="antecedent">The task whose completion will execute the continuation.</param>
			public ExecuteContinuationSynchronouslyAwaitable(Task antecedent)
			{
				Guard.EnsureNotNull(antecedent, nameof(antecedent));
				this.antecedent = antecedent;
			}

			/// <summary>
			/// Gets the awaiter.
			/// </summary>
			/// <returns>The awaiter.</returns>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
			public ExecuteContinuationSynchronouslyAwaiter GetAwaiter() => new ExecuteContinuationSynchronouslyAwaiter(this.antecedent);
		}

		/// <summary>
		/// A Task awaiter that has affinity to executing callbacks synchronously on the completing callstack.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		internal readonly struct ExecuteContinuationSynchronouslyAwaiter : INotifyCompletion, IAwaiter
		{
			/// <summary>
			/// The task whose completion will execute the continuation.
			/// </summary>
			private readonly Task antecedent;

			/// <summary>
			/// Initializes a new instance of the <see cref="ExecuteContinuationSynchronouslyAwaiter"/> struct.
			/// </summary>
			/// <param name="antecedent">The task whose completion will execute the continuation.</param>
			public ExecuteContinuationSynchronouslyAwaiter(Task antecedent)
			{
				Guard.EnsureNotNull(antecedent, nameof(antecedent));
				this.antecedent = antecedent;
			}

			/// <summary>
			/// Gets a value indicating whether the antedent <see cref="Task"/> has already completed.
			/// </summary>
			public bool IsCompleted => antecedent.IsCompleted;

			/// <summary>
			/// Rethrows any exception thrown by the antecedent.
			/// </summary>
			public void GetResult() => antecedent.GetAwaiter().GetResult();

			/// <summary>
			/// Schedules a callback to run when the antecedent task completes.
			/// </summary>
			/// <param name="continuation">The callback to invoke.</param>
			public void OnCompleted(Action continuation)
			{
				Guard.EnsureNotNull(continuation, nameof(continuation));

				antecedent.ContinueWith(
					(_, s) => ((Action)s)(),
					continuation,
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
			}
		}

		/// <summary>
		/// A Task awaitable that has affinity to executing callbacks synchronously on the completing callstack.
		/// </summary>
		/// <typeparam name="T">The type of value returned by the awaited <see cref="Task"/>.</typeparam>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		readonly struct ExecuteContinuationSynchronouslyAwaitable<T>
		{
			/// <summary>
			/// The task whose completion will execute the continuation.
			/// </summary>
			private readonly Task<T> antecedent;

			/// <summary>
			/// Initializes a new instance of the <see cref="ExecuteContinuationSynchronouslyAwaitable{T}"/> struct.
			/// </summary>
			/// <param name="antecedent">The task whose completion will execute the continuation.</param>
			public ExecuteContinuationSynchronouslyAwaitable(Task<T> antecedent)
			{
				Guard.EnsureNotNull(antecedent, nameof(antecedent));
				this.antecedent = antecedent;
			}

			/// <summary>
			/// Gets the awaiter.
			/// </summary>
			/// <returns>The awaiter.</returns>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
			public ExecuteContinuationSynchronouslyAwaiter<T> GetAwaiter() => new ExecuteContinuationSynchronouslyAwaiter<T>(antecedent);
		}

		/// <summary>
		/// A Task awaiter that has affinity to executing callbacks synchronously on the completing callstack.
		/// </summary>
		/// <typeparam name="T">The type of value returned by the awaited <see cref="Task"/>.</typeparam>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
		readonly struct ExecuteContinuationSynchronouslyAwaiter<T> : INotifyCompletion, IAwaiter<T>
		{
			/// <summary>
			/// The task whose completion will execute the continuation.
			/// </summary>
			private readonly Task<T> antecedent;

			/// <summary>
			/// Initializes a new instance of the <see cref="ExecuteContinuationSynchronouslyAwaiter{T}"/> struct.
			/// </summary>
			/// <param name="antecedent">The task whose completion will execute the continuation.</param>
			public ExecuteContinuationSynchronouslyAwaiter(Task<T> antecedent)
			{
				Guard.EnsureNotNull(antecedent, nameof(antecedent));
				this.antecedent = antecedent;
			}

			/// <summary>
			/// Gets a value indicating whether the antedent <see cref="Task"/> has already completed.
			/// </summary>
			public bool IsCompleted => antecedent.IsCompleted;

			void IAwaiter.GetResult() => antecedent.GetAwaiter().GetResult();

			/// <summary>
			/// Rethrows any exception thrown by the antecedent.
			/// </summary>
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
			public T GetResult() => antecedent.GetAwaiter().GetResult();

			/// <summary>
			/// Schedules a callback to run when the antecedent task completes.
			/// </summary>
			/// <param name="continuation">The callback to invoke.</param>
			public void OnCompleted(Action continuation)
			{
				Guard.EnsureNotNull(continuation, nameof(continuation));

				antecedent.ContinueWith(
					(_, s) => ((Action)s)(),
					continuation,
					CancellationToken.None,
					TaskContinuationOptions.ExecuteSynchronously,
					TaskScheduler.Default);
			}
		}
	}
}

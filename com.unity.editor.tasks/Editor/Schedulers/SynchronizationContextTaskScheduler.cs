//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: SynchronizationContextTaskScheduler.cs
//
//--------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	/// <summary>Provides a task scheduler that targets a specific SynchronizationContext.</summary>
	public sealed class SynchronizationContextTaskScheduler : TaskScheduler, IDisposable
	{
		/// <summary>The target context under which to execute the queued tasks.</summary>
		public SynchronizationContext Context { get; }
		private readonly ConcurrentDictionary<Task, byte> tasks = new ConcurrentDictionary<Task, byte>();

		/// <summary>Initializes an instance of the SynchronizationContextTaskScheduler class.</summary>
		public SynchronizationContextTaskScheduler() : this(SynchronizationContext.Current) {}

		/// <summary>
		/// Initializes an instance of the SynchronizationContextTaskScheduler class
		/// with the specified SynchronizationContext.
		/// </summary>
		/// <param name="context">The SynchronizationContext under which to execute tasks.</param>
		public SynchronizationContextTaskScheduler(SynchronizationContext context)
		{
			if (context == null) throw new ArgumentNullException(nameof(context));
			this.Context = context;
		}

		/// <summary>Queues a task to the scheduler for execution.</summary>
		/// <param name="task">The Task to queue.</param>
		protected override void QueueTask(Task task)
		{
			if (disposed) return;

			tasks.TryAdd(task, 0);
			Context.Post(delegate(object state) {
				var t = (Task)state;
				tasks.TryRemove(t, out var _);
				TryExecuteTask(t);
			}, task);
		}

		/// <summary>Tries to execute a task on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued">Ignored.</param>
		/// <returns>Whether the task could be executed.</returns>
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			if (disposed) return false;
			return Context == SynchronizationContext.Current && TryExecuteTask(task);
		} 

		protected override IEnumerable<Task> GetScheduledTasks() => tasks.Keys;

		/// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
		public override int MaximumConcurrencyLevel { get; } = 1;

		private bool disposed;
		public void Dispose()
		{
			disposed = true;
		}
	}
}

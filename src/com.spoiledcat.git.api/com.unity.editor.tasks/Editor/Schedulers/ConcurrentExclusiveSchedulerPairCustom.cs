// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ConcurrentExclusiveSchedulerPair.cs
// https://referencesource.microsoft.com/#mscorlib/system/threading/Tasks/ConcurrentExclusiveSchedulerPair.cs
//
// <OWNER>Microsoft</OWNER>
//
// A pair of schedulers that together support concurrent (reader) / exclusive (writer)
// task scheduling.  Using just the exclusive scheduler can be used to simulate a serial
// processing queue, and using just the concurrent scheduler with a specified
// MaximumConcurrentlyLevel can be used to achieve a MaxDegreeOfParallelism across
// a bunch of tasks, parallel loops, dataflow blocks, etc.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using Helpers;

	/// <summary>
	/// Provides concurrent and exclusive task schedulers that coordinate to execute
	/// tasks while ensuring that concurrent tasks may run concurrently and exclusive tasks never do.
	/// </summary>
	[DebuggerDisplay("Concurrent={ConcurrentTaskCountForDebugger}, Exclusive={ExclusiveTaskCountForDebugger}, Mode={ModeForDebugger}")]
	[DebuggerTypeProxy(typeof(ConcurrentExclusiveSchedulerPairCustom.DebugView))]
	public class ConcurrentExclusiveSchedulerPairCustom
	{
		private readonly CancellationTokenSource cts;
		/// <summary>A dictionary mapping thread ID to a processing mode to denote what kinds of tasks are currently being processed on this thread.</summary>
		private readonly ConcurrentDictionary<int, ProcessingMode> m_threadProcessingMapping = new ConcurrentDictionary<int, ProcessingMode>();
		/// <summary>The scheduler used to queue and execute "concurrent" tasks that may run concurrently with other concurrent tasks.</summary>
		private readonly ConcurrentExclusiveTaskScheduler m_concurrentTaskScheduler;
		/// <summary>The scheduler used to queue and execute "exclusive" tasks that must run exclusively while no other tasks for this pair are running.</summary>
		private readonly ConcurrentExclusiveTaskScheduler m_exclusiveTaskScheduler;
		/// <summary>The underlying task scheduler to which all work should be scheduled.</summary>
		private readonly TaskScheduler m_underlyingTaskScheduler;
		/// <summary>
		/// The maximum number of tasks allowed to run concurrently.  This only applies to concurrent tasks,
		/// since exlusive tasks are inherently limited to 1.
		/// </summary>
		private readonly int m_maxConcurrencyLevel;
		/// <summary>The maximum number of tasks we can process before recyling our runner tasks.</summary>
		private readonly int m_maxItemsPerTask;
		/// <summary>
		/// If positive, it represents the number of concurrently running concurrent tasks.
		/// If negative, it means an exclusive task has been scheduled.
		/// If 0, nothing has been scheduled.
		/// </summary>
		private int m_processingCount;
		/// <summary>Completion state for a task representing the completion of this pair.</summary>
		/// <remarks>Lazily-initialized only if the scheduler pair is shutting down or if the Completion is requested.</remarks>
		private CompletionState m_completionState;


		/// <summary>A constant value used to signal unlimited processing.</summary>
		private const int UNLIMITED_PROCESSING = -1;
		/// <summary>Constant used for m_processingCount to indicate that an exclusive task is being processed.</summary>
		private const int EXCLUSIVE_PROCESSING_SENTINEL = -1;
		/// <summary>Default MaxItemsPerTask to use for processing if none is specified.</summary>
		private const int DEFAULT_MAXITEMSPERTASK = UNLIMITED_PROCESSING;
		/// <summary>Default MaxConcurrencyLevel is the processor count if not otherwise specified.</summary>
		private static Int32 DefaultMaxConcurrencyLevel => Environment.ProcessorCount;

		/// <summary>Gets the sync obj used to protect all state on this instance.</summary>
		private object ValueLock => m_threadProcessingMapping;

		private CancellationToken Token => cts.Token;

		/// <summary>
		/// Initializes the ConcurrentExclusiveSchedulerCustom.
		/// </summary>
		public ConcurrentExclusiveSchedulerPairCustom(CancellationToken token) :
			this(token, TaskScheduler.Default, DefaultMaxConcurrencyLevel, DEFAULT_MAXITEMSPERTASK)
		{ }

		/// <summary>
		/// Initializes the ConcurrentExclusiveSchedulerCustom to target the specified scheduler with a maximum
		/// concurrency level and a maximum number of scheduled tasks that may be processed as a unit.
		/// </summary>
		/// <param name="token"></param>
		/// <param name="taskScheduler">The target scheduler on which this pair should execute.</param>
		/// <param name="maxConcurrencyLevel">The maximum number of tasks to run concurrently.</param>
		/// <param name="maxItemsPerTask">The maximum number of tasks to process for each underlying scheduled task used by the pair.</param>
		public ConcurrentExclusiveSchedulerPairCustom(CancellationToken token, TaskScheduler taskScheduler, int maxConcurrencyLevel, int maxItemsPerTask)
		{
			// Validate arguments
			if (taskScheduler == null) throw new ArgumentNullException(nameof(taskScheduler));
			if (maxConcurrencyLevel == 0 || maxConcurrencyLevel < -1) throw new ArgumentOutOfRangeException(nameof(maxConcurrencyLevel));
			if (maxItemsPerTask == 0 || maxItemsPerTask < -1) throw new ArgumentOutOfRangeException(nameof(maxItemsPerTask));
			Contract.EndContractBlock();

			cts = CancellationTokenSource.CreateLinkedTokenSource(token);

			// Store configuration
			m_underlyingTaskScheduler = taskScheduler;
			m_maxConcurrencyLevel = maxConcurrencyLevel;
			m_maxItemsPerTask = maxItemsPerTask;

			// Downgrade to the underlying scheduler's max degree of parallelism if it's lower than the user-supplied level
			int mcl = taskScheduler.MaximumConcurrencyLevel;
			if (mcl > 0 && mcl < m_maxConcurrencyLevel) m_maxConcurrencyLevel = mcl;

			// Treat UNLIMITED_PROCESSING/-1 for both MCL and MIPT as the biggest possible value so that we don't
			// have to special case UNLIMITED_PROCESSING later on in processing.
			if (m_maxConcurrencyLevel == UNLIMITED_PROCESSING) m_maxConcurrencyLevel = Int32.MaxValue;
			if (m_maxItemsPerTask == UNLIMITED_PROCESSING) m_maxItemsPerTask = Int32.MaxValue;

			// Create the concurrent/exclusive schedulers for this pair
			m_exclusiveTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, 1, ProcessingMode.ProcessingExclusiveTask);
			m_concurrentTaskScheduler = new ConcurrentExclusiveTaskScheduler(this, m_maxConcurrencyLevel, ProcessingMode.ProcessingConcurrentTasks);
		}

		/// <summary>Informs the scheduler pair that it should not accept any more tasks.</summary>
		/// <remarks>
		/// Calling <see cref="Complete"/> is optional, and it's only necessary if the <see cref="Completion"/>
		/// will be relied on for notification of all processing being completed.
		/// </remarks>
		public void Complete()
		{
			lock (ValueLock)
			{
				if (!CompletionRequested)
				{
					RequestCompletion();
					CleanupStateIfCompletingAndQuiesced();
				}
			}
		}

		/// <summary>Gets a <see cref="System.Threading.Tasks.Task"/> that will complete when the scheduler has completed processing.</summary>
		public Task Completion
		{
			// ValueLock not needed, but it's ok if it's held
			get { return EnsureCompletionStateInitialized().Task; }
		}

		/// <summary>Gets the lazily-initialized completion state.</summary>
		private CompletionState EnsureCompletionStateInitialized()
		{
			// ValueLock not needed, but it's ok if it's held
			return LazyInitializer.EnsureInitialized(ref m_completionState, () => new CompletionState());
		}

		/// <summary>Gets whether completion has been requested.</summary>
		private bool CompletionRequested
		{
			// ValueLock not needed, but it's ok if it's held
			get { return m_completionState != null && Volatile.Read(ref m_completionState.m_completionRequested); }
		}

		/// <summary>Sets that completion has been requested.</summary>
		private void RequestCompletion()
		{
			ContractAssertMonitorStatus(ValueLock, held: true);
			EnsureCompletionStateInitialized().m_completionRequested = true;
		}

		/// <summary>
		/// Cleans up state if and only if there's no processing currently happening
		/// and no more to be done later.
		/// </summary>
		private void CleanupStateIfCompletingAndQuiesced()
		{
			ContractAssertMonitorStatus(ValueLock, held: true);
			if (ReadyToComplete) CompleteTaskAsync();
		}

		/// <summary>Gets whether the pair is ready to complete.</summary>
		private bool ReadyToComplete
		{
			get
			{
				ContractAssertMonitorStatus(ValueLock, held: true);

				// We can only complete if completion has been requested and no processing is currently happening.
				if (!CompletionRequested || m_processingCount != 0) return false;

				// Now, only allow shutdown if an exception occurred or if there are no more tasks to process.
				var cs = EnsureCompletionStateInitialized();
				return
					(cs.m_exceptions != null && cs.m_exceptions.Count > 0) ||
					(m_concurrentTaskScheduler.m_tasks.IsEmpty && m_exclusiveTaskScheduler.m_tasks.IsEmpty);
			}
		}

		/// <summary>Completes the completion task asynchronously.</summary>
		private void CompleteTaskAsync()
		{
			Contract.Requires(ReadyToComplete, "The block must be ready to complete to be here.");
			ContractAssertMonitorStatus(ValueLock, held: true);

			// Ensure we only try to complete once, then schedule completion
			// in order to escape held locks and the caller's context
			var cs = EnsureCompletionStateInitialized();
			if (!cs.m_completionQueued)
			{
				cs.m_completionQueued = true;
				ThreadPool.QueueUserWorkItem(state => {
					var localCs = (CompletionState)state; // don't use 'cs', as it'll force a closure
					Contract.Assert(!localCs.Task.IsCompleted, "Completion should only happen once.");

					var exceptions = localCs.m_exceptions;
					bool success = (exceptions != null && exceptions.Count > 0) ?
						localCs.TrySetException(exceptions) :
						localCs.TrySetResult(default);
					Contract.Assert(success, "Expected to complete completion task.");
				}, cs);
			}
		}

		/// <summary>Initiatites scheduler shutdown due to a worker task faulting..</summary>
		/// <param name="faultedTask">The faulted worker task that's initiating the shutdown.</param>
		private void FaultWithTask(Task faultedTask)
		{
			Contract.Requires(faultedTask != null && faultedTask.IsFaulted && faultedTask.Exception.InnerExceptions.Count > 0,
				"Needs a task in the faulted state and thus with exceptions.");
			ContractAssertMonitorStatus(ValueLock, held: true);

			// Store the faulted task's exceptions
			var cs = EnsureCompletionStateInitialized();
			if (cs.m_exceptions == null) cs.m_exceptions = new List<Exception>();
			cs.m_exceptions.AddRange(faultedTask.Exception.InnerExceptions);

			// Now that we're doomed, request completion
			RequestCompletion();
		}

		/// <summary>
		/// Gets a TaskScheduler that can be used to schedule tasks to this pair
		/// that may run concurrently with other tasks on this pair.
		/// </summary>
		public TaskScheduler ConcurrentScheduler { get { return m_concurrentTaskScheduler; } }
		/// <summary>
		/// Gets a TaskScheduler that can be used to schedule tasks to this pair
		/// that must run exclusively with regards to other tasks on this pair.
		/// </summary>
		public TaskScheduler ExclusiveScheduler { get { return m_exclusiveTaskScheduler; } }

		/// <summary>Gets the number of tasks waiting to run concurrently.</summary>
		/// <remarks>This does not take the necessary lock, as it's only called from under the debugger.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		private int ConcurrentTaskCountForDebugger { get { return m_concurrentTaskScheduler.m_tasks.Count; } }

		/// <summary>Gets the number of tasks waiting to run exclusively.</summary>
		/// <remarks>This does not take the necessary lock, as it's only called from under the debugger.</remarks>
		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		private int ExclusiveTaskCountForDebugger { get { return m_exclusiveTaskScheduler.m_tasks.Count; } }

		/// <summary>Notifies the pair that new work has arrived to be processed.</summary>
		/// <param name="fairly">Whether tasks should be scheduled fairly with regards to other tasks.</param>
		/// <remarks>Must only be called while holding the lock.</remarks>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		[SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals")]
		[SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		private void ProcessAsyncIfNecessary(bool fairly = false)
		{
			ContractAssertMonitorStatus(ValueLock, held: true);

			// If the current processing count is >= 0, we can potentially launch further processing.
			if (m_processingCount >= 0)
			{
				// We snap whether there are any exclusive tasks or concurrent tasks waiting.
				// (We grab the concurrent count below only once we know we need it.)
				// With processing happening concurrent to this operation, this data may
				// immediately be out of date, but it can only go from non-empty
				// to empty and not the other way around.  As such, this is safe,
				// as worst case is we'll schedule an extra  task when we didn't
				// otherwise need to, and we'll just eat its overhead.
				bool exclusiveTasksAreWaiting = !m_exclusiveTaskScheduler.m_tasks.IsEmpty;

				// If there's no processing currently happening but there are waiting exclusive tasks,
				// let's start processing those exclusive tasks.
				Task processingTask = null;
				if (m_processingCount == 0 && exclusiveTasksAreWaiting)
				{
					// Launch exclusive task processing
					m_processingCount = EXCLUSIVE_PROCESSING_SENTINEL; // -1
					try
					{
						processingTask = new Task(thisPair => ((ConcurrentExclusiveSchedulerPairCustom)thisPair).ProcessExclusiveTasks(), this,
							Token, GetCreationOptionsForTask(fairly));
						processingTask.Start(m_underlyingTaskScheduler);
						// When we call Start, if the underlying scheduler throws in QueueTask, TPL will fault the task and rethrow
						// the exception.  To deal with that, we need a reference to the task object, so that we can observe its exception.
						// Hence, we separate creation and starting, so that we can store a reference to the task before we attempt QueueTask.
					}
					catch
					{
						m_processingCount = 0;
						FaultWithTask(processingTask);
					}
				}
				// If there are no waiting exclusive tasks, there are concurrent tasks, and we haven't reached our maximum
				// concurrency level for processing, let's start processing more concurrent tasks.
				else
				{
					int concurrentTasksWaitingCount = m_concurrentTaskScheduler.m_tasks.Count;

					if (concurrentTasksWaitingCount > 0 && !exclusiveTasksAreWaiting && m_processingCount < m_maxConcurrencyLevel)
					{
						// Launch concurrent task processing, up to the allowed limit
						for (int i = 0; i < concurrentTasksWaitingCount && m_processingCount < m_maxConcurrencyLevel; ++i)
						{
							++m_processingCount;
							try
							{
								processingTask = new Task(thisPair => ((ConcurrentExclusiveSchedulerPairCustom)thisPair).ProcessConcurrentTasks(), this,
									Token, GetCreationOptionsForTask(fairly));
								processingTask.Start(m_underlyingTaskScheduler); // See above logic for why we use new + Start rather than StartNew
							}
							catch
							{
								--m_processingCount;
								FaultWithTask(processingTask);
							}
						}
					}
				}

				// Check to see if all tasks have completed and if completion has been requested.
				CleanupStateIfCompletingAndQuiesced();
			}
			else Contract.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "The processing count must be the sentinel if it's not >= 0.");
		}

		/// <summary>
		/// Processes exclusive tasks serially until either there are no more to process
		/// or we've reached our user-specified maximum limit.
		/// </summary>
		private void ProcessExclusiveTasks()
		{
			Contract.Requires(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "Processing exclusive tasks requires being in exclusive mode.");
			Contract.Requires(!m_exclusiveTaskScheduler.m_tasks.IsEmpty, "Processing exclusive tasks requires tasks to be processed.");
			ContractAssertMonitorStatus(ValueLock, held: false);
			try
			{
				// Note that we're processing exclusive tasks on the current thread
				Contract.Assert(!m_threadProcessingMapping.ContainsKey(Thread.CurrentThread.ManagedThreadId),
					"This thread should not yet be involved in this pair's processing.");
				m_threadProcessingMapping[Thread.CurrentThread.ManagedThreadId] = ProcessingMode.ProcessingExclusiveTask;

				// Process up to the maximum number of items per task allowed
				for (int i = 0; i < m_maxItemsPerTask; i++)
				{
					// Get the next available exclusive task.  If we can't find one, bail.
					Task exclusiveTask;
					if (!m_exclusiveTaskScheduler.m_tasks.TryDequeue(out exclusiveTask)) break;

					// Execute the task.  If the scheduler was previously faulted,
					// this task could have been faulted when it was queued; ignore such tasks.
					if (!exclusiveTask.IsFaulted) m_exclusiveTaskScheduler.ExecuteTask(exclusiveTask);
				}
			}
			finally
			{
				// We're no longer processing exclusive tasks on the current thread
				ProcessingMode currentMode;
				m_threadProcessingMapping.TryRemove(Thread.CurrentThread.ManagedThreadId, out currentMode);
				Contract.Assert(currentMode == ProcessingMode.ProcessingExclusiveTask,
					"Somehow we ended up escaping exclusive mode.");

				lock (ValueLock)
				{
					// When this task was launched, we tracked it by setting m_processingCount to WRITER_IN_PROGRESS.
					// now reset it to 0.  Then check to see whether there's more processing to be done.
					// There might be more concurrent tasks available, for example, if concurrent tasks arrived
					// after we exited the loop, or if we exited the loop while concurrent tasks were still
					// available but we hit our maxItemsPerTask limit.
					Contract.Assert(m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL, "The processing mode should not have deviated from exclusive.");
					m_processingCount = 0;
					ProcessAsyncIfNecessary(true);
				}
			}
		}

		/// <summary>
		/// Processes concurrent tasks serially until either there are no more to process,
		/// we've reached our user-specified maximum limit, or exclusive tasks have arrived.
		/// </summary>
		private void ProcessConcurrentTasks()
		{
			Contract.Requires(m_processingCount > 0, "Processing concurrent tasks requires us to be in concurrent mode.");
			ContractAssertMonitorStatus(ValueLock, held: false);
			try
			{
				// Note that we're processing concurrent tasks on the current thread
				Contract.Assert(!m_threadProcessingMapping.ContainsKey(Thread.CurrentThread.ManagedThreadId),
					"This thread should not yet be involved in this pair's processing.");
				m_threadProcessingMapping[Thread.CurrentThread.ManagedThreadId] = ProcessingMode.ProcessingConcurrentTasks;

				// Process up to the maximum number of items per task allowed
				for (int i = 0; i < m_maxItemsPerTask; i++)
				{
					// Get the next available concurrent task.  If we can't find one, bail.
					Task concurrentTask;
					if (!m_concurrentTaskScheduler.m_tasks.TryDequeue(out concurrentTask)) break;

					// Execute the task.  If the scheduler was previously faulted,
					// this task could have been faulted when it was queued; ignore such tasks.
					if (!concurrentTask.IsFaulted) m_concurrentTaskScheduler.ExecuteTask(concurrentTask);

					// Now check to see if exclusive tasks have arrived; if any have, they take priority
					// so we'll bail out here.  Note that we could have checked this condition
					// in the for loop's condition, but that could lead to extra overhead
					// in the case where a concurrent task arrives, this task is launched, and then
					// before entering the loop an exclusive task arrives.  If we didn't execute at
					// least one task, we would have spent all of the overhead to launch a
					// task but with none of the benefit.  There's of course also an inherent
					// ---- here with regards to exclusive tasks arriving, and we're ok with
					// executing one more concurrent task than we should before giving priority to exclusive tasks.
					if (!m_exclusiveTaskScheduler.m_tasks.IsEmpty) break;
				}
			}
			finally
			{
				// We're no longer processing concurrent tasks on the current thread
				ProcessingMode currentMode;
				m_threadProcessingMapping.TryRemove(Thread.CurrentThread.ManagedThreadId, out currentMode);
				Contract.Assert(currentMode == ProcessingMode.ProcessingConcurrentTasks,
					"Somehow we ended up escaping concurrent mode.");

				lock (ValueLock)
				{
					// When this task was launched, we tracked it with a positive processing count;
					// decrement that count.  Then check to see whether there's more processing to be done.
					// There might be more concurrent tasks available, for example, if concurrent tasks arrived
					// after we exited the loop, or if we exited the loop while concurrent tasks were still
					// available but we hit our maxItemsPerTask limit.
					Contract.Assert(m_processingCount > 0, "The procesing mode should not have deviated from concurrent.");
					if (m_processingCount > 0) --m_processingCount;
					ProcessAsyncIfNecessary(true);
				}
			}
		}

#if PRENET45
		/// <summary>
		/// Type used with TaskCompletionSource(Of TResult) as the TResult
		/// to ensure that the resulting task can't be upcast to something
		/// that in the future could lead to compat problems.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
		[DebuggerNonUserCode]
		private struct VoidTaskResult { }
#endif

		/// <summary>
		/// Holder for lazily-initialized state about the completion of a scheduler pair.
		/// Completion is only triggered either by rare exceptional conditions or by
		/// the user calling Complete, and as such we only lazily initialize this
		/// state in one of those conditions or if the user explicitly asks for
		/// the Completion.
		/// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
		private sealed class CompletionState : TaskCompletionSource<bool>
		{
			/// <summary>Whether completion processing has been queued.</summary>
			public bool m_completionQueued;
			/// <summary>Whether the scheduler has had completion requested.</summary>
			/// <remarks>This variable is not volatile, so to gurantee safe reading reads, Volatile.Read is used in TryExecuteTaskInline.</remarks>
			public bool m_completionRequested;
			/// <summary>Unrecoverable exceptions incurred while processing.</summary>
			public List<Exception> m_exceptions;
		}

		/// <summary>
		/// A scheduler shim used to queue tasks to the pair and execute those tasks on request of the pair.
		/// </summary>
		[DebuggerDisplay("Count={CountForDebugger}, MaxConcurrencyLevel={m_maxConcurrencyLevel}, Id={Id}")]
		[DebuggerTypeProxy(typeof(ConcurrentExclusiveTaskScheduler.DebugView))]
		private sealed class ConcurrentExclusiveTaskScheduler : TaskScheduler
		{
			/// <summary>Cached delegate for invoking TryExecuteTaskShim.</summary>
			private static readonly Func<object, bool> s_tryExecuteTaskShim = new Func<object, bool>(TryExecuteTaskShim);
			/// <summary>The maximum concurrency level for the scheduler.</summary>
			private readonly int m_maxConcurrencyLevel;
			/// <summary>The parent pair.</summary>
			private readonly ConcurrentExclusiveSchedulerPairCustom m_pair;
			/// <summary>The processing mode of this scheduler, exclusive or concurrent.</summary>
			private readonly ProcessingMode m_processingMode;
			/// <summary>Gets the queue of tasks for this scheduler.</summary>
			public readonly IProducerConsumerQueue<Task> m_tasks;

			/// <summary>Initializes the scheduler.</summary>
			/// <param name="pair">The parent pair.</param>
			/// <param name="maxConcurrencyLevel">The maximum degree of concurrency this scheduler may use.</param>
			/// <param name="processingMode">The processing mode of this scheduler.</param>
			public ConcurrentExclusiveTaskScheduler(ConcurrentExclusiveSchedulerPairCustom pair, int maxConcurrencyLevel, ProcessingMode processingMode)
			{
				Contract.Requires(pair != null, "Scheduler must be associated with a valid pair.");
				Contract.Requires(processingMode == ProcessingMode.ProcessingConcurrentTasks || processingMode == ProcessingMode.ProcessingExclusiveTask,
					"Scheduler must be for concurrent or exclusive processing.");
				Contract.Requires(
					(processingMode == ProcessingMode.ProcessingConcurrentTasks && (maxConcurrencyLevel >= 1 || maxConcurrencyLevel == UNLIMITED_PROCESSING)) ||
					(processingMode == ProcessingMode.ProcessingExclusiveTask && maxConcurrencyLevel == 1),
					"If we're in concurrent mode, our concurrency level should be positive or unlimited.  If exclusive, it should be 1.");

				m_pair = pair;
				m_maxConcurrencyLevel = maxConcurrencyLevel;
				m_processingMode = processingMode;
				m_tasks = (processingMode == ProcessingMode.ProcessingExclusiveTask) ?
					(IProducerConsumerQueue<Task>)new SingleProducerSingleConsumerQueue<Task>() :
					(IProducerConsumerQueue<Task>)new MultiProducerMultiConsumerQueue<Task>();
			}

			/// <summary>Executes a task on this scheduler.</summary>
			/// <param name="task">The task to be executed.</param>
			[SecuritySafeCritical]
			public void ExecuteTask(Task task)
			{
				Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
				base.TryExecuteTask(task);
			}

			/// <summary>Queues a task to the scheduler.</summary>
			/// <param name="task">The task to be queued.</param>
			[SecurityCritical]
			protected override void QueueTask(Task task)
			{
				Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");
				lock (m_pair.ValueLock)
				{
					// If the scheduler has already had completion requested, no new work is allowed to be scheduled
					if (m_pair.CompletionRequested) throw new InvalidOperationException(GetType().Name);

					// Queue the task, and then let the pair know that more work is now available to be scheduled
					m_tasks.Enqueue(task);
					m_pair.ProcessAsyncIfNecessary();
				}
			}

			/// <summary>Tries to execute the task synchronously on this scheduler.</summary>
			/// <param name="task">The task to execute.</param>
			/// <param name="taskWasPreviouslyQueued">Whether the task was previously queued to the scheduler.</param>
			/// <returns>true if the task could be executed; otherwise, false.</returns>
			[SecurityCritical]
			protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
			{
				Contract.Assert(task != null, "Infrastructure should have provided a non-null task.");

				// If the scheduler has had completion requested, no new work is allowed to be scheduled.
				// A non-locked read on m_completionRequested (in CompletionRequested) is acceptable here because:
				// a) we don't need to be exact... a Complete call could come in later in the function anyway
				// b) this is only a fast path escape hatch.  To actually inline the task,
				//    we need to be inside of an already executing task, and in such a case,
				//    while completion may have been requested, we can't have shutdown yet.
				if (!taskWasPreviouslyQueued && m_pair.CompletionRequested) return false;

				// We know the implementation of the default scheduler and how it will behave.
				// As it's the most common underlying scheduler, we optimize for it.
				bool isDefaultScheduler = m_pair.m_underlyingTaskScheduler == TaskScheduler.Default;

				// If we're targeting the default scheduler and taskWasPreviouslyQueued is true,
				// we know that the default scheduler will only allow it to be inlined
				// if we're on a thread pool thread (but it won't always allow it in that case,
				// since it'll only allow inlining if it can find the task in the local queue).
				// As such, if we're not on a thread pool thread, we know for sure the
				// task won't be inlined, so let's not even try.
				if (isDefaultScheduler && taskWasPreviouslyQueued && !Thread.CurrentThread.IsThreadPoolThread)
				{
					return false;
				}
				else
				{
					// If a task is already running on this thread, allow inline execution to proceed.
					// If there's already a task from this scheduler running on the current thread, we know it's safe
					// to run this task, in effect temporarily taking that task's count allocation.
					ProcessingMode currentThreadMode;
					if (m_pair.m_threadProcessingMapping.TryGetValue(Thread.CurrentThread.ManagedThreadId, out currentThreadMode) &&
						currentThreadMode == m_processingMode)
					{
						// If we're targeting the default scheduler and taskWasPreviouslyQueued is false,
						// we know the default scheduler will allow it, so we can just execute it here.
						// Otherwise, delegate to the target scheduler's inlining.
						return (isDefaultScheduler && !taskWasPreviouslyQueued) ?
							TryExecuteTask(task) :
							TryExecuteTaskInlineOnTargetScheduler(task);
					}
				}

				// We're not in the context of a task already executing on this scheduler.  Bail.
				return false;
			}

			/// <summary>Gets for debugging purposes the tasks scheduled to this scheduler.</summary>
			/// <returns>An enumerable of the tasks queued.</returns>
			[SecurityCritical]
			protected override IEnumerable<Task> GetScheduledTasks() { return m_tasks; }

			/// <summary>
			/// Implements a reasonable approximation for TryExecuteTaskInline on the underlying scheduler,
			/// which we can't call directly on the underlying scheduler.
			/// </summary>
			/// <param name="task">The task to execute inline if possible.</param>
			/// <returns>true if the task was inlined successfully; otherwise, false.</returns>
			[SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "ignored")]
			private bool TryExecuteTaskInlineOnTargetScheduler(Task task)
			{
				// We'd like to simply call TryExecuteTaskInline here, but we can't.
				// As there's no built-in API for this, a workaround is to create a new task that,
				// when executed, will simply call TryExecuteTask to run the real task, and then
				// we run our new shim task synchronously on the target scheduler.  If all goes well,
				// our synchronous invocation will succeed in running the shim task on the current thread,
				// which will in turn run the real task on the current thread.  If the scheduler
				// doesn't allow that execution, RunSynchronously will block until the underlying scheduler
				// is able to invoke the task, which might account for an additional but unavoidable delay.
				// Once it's done, we can return whether the task executed by returning the
				// shim task's Result, which is in turn the result of TryExecuteTask.
				var t = new Task<bool>(s_tryExecuteTaskShim, Tuple.Create(this, task));
				try
				{
					t.RunSynchronously(m_pair.m_underlyingTaskScheduler);
					return t.Result;
				}
				catch
				{
					Contract.Assert(t.IsFaulted, "Task should be faulted due to the scheduler faulting it and throwing the exception.");
					var ignored = t.Exception;
					throw;
				}
				finally { t.Dispose(); }
			}

			/// <summary>Shim used to invoke this.TryExecuteTask(task).</summary>
			/// <param name="state">A tuple of the ConcurrentExclusiveTaskScheduler and the task to execute.</param>
			/// <returns>true if the task was successfully inlined; otherwise, false.</returns>
			/// <remarks>
			/// This method is separated out not because of performance reasons but so that
			/// the SecuritySafeCritical attribute may be employed.
			/// </remarks>
			[SecuritySafeCritical]
			private static bool TryExecuteTaskShim(object state)
			{
				var tuple = (Tuple<ConcurrentExclusiveTaskScheduler, Task>)state;
				return tuple.Item1.TryExecuteTask(tuple.Item2);
			}

			/// <summary>Gets the maximum concurrency level this scheduler is able to support.</summary>
			public override int MaximumConcurrencyLevel { get { return m_maxConcurrencyLevel; } }

			public IEnumerable<Task> Tasks => m_tasks;

			/// <summary>Gets the number of tasks queued to this scheduler.</summary>
			[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
			private int CountForDebugger { get { return m_tasks.Count; } }

			/// <summary>Provides a debug view for ConcurrentExclusiveTaskScheduler.</summary>
			private sealed class DebugView
			{
				/// <summary>The scheduler being debugged.</summary>
				private readonly ConcurrentExclusiveTaskScheduler m_taskScheduler;

				/// <summary>Initializes the debug view.</summary>
				/// <param name="scheduler">The scheduler being debugged.</param>
				public DebugView(ConcurrentExclusiveTaskScheduler scheduler)
				{
					Contract.Requires(scheduler != null, "Need a scheduler with which to construct the debug view.");
					m_taskScheduler = scheduler;
				}

				/// <summary>Gets this pair's maximum allowed concurrency level.</summary>
				public int MaximumConcurrencyLevel { get { return m_taskScheduler.m_maxConcurrencyLevel; } }

				/// <summary>Gets the tasks scheduled to this scheduler.</summary>
				public IEnumerable<Task> ScheduledTasks { get { return m_taskScheduler.m_tasks; } }

				/// <summary>Gets the scheduler pair with which this scheduler is associated.</summary>
				public ConcurrentExclusiveSchedulerPairCustom SchedulerPair { get { return m_taskScheduler.m_pair; } }
			}
		}

		/// <summary>Provides a debug view for ConcurrentExclusiveSchedulerCustom.</summary>
		private sealed class DebugView
		{
			/// <summary>The pair being debugged.</summary>
			private readonly ConcurrentExclusiveSchedulerPairCustom m_pair;

			/// <summary>Initializes the debug view.</summary>
			/// <param name="pair">The pair being debugged.</param>
			public DebugView(ConcurrentExclusiveSchedulerPairCustom pair)
			{
				Contract.Requires(pair != null, "Need a pair with which to construct the debug view.");
				m_pair = pair;
			}

			/// <summary>Gets a representation of the execution state of the pair.</summary>
			public ProcessingMode Mode { get { return m_pair.ModeForDebugger; } }

			/// <summary>Gets the number of tasks waiting to run exclusively.</summary>
			public IEnumerable<Task> ScheduledExclusive { get { return m_pair.m_exclusiveTaskScheduler.m_tasks; } }

			/// <summary>Gets the number of tasks waiting to run concurrently.</summary>
			public IEnumerable<Task> ScheduledConcurrent { get { return m_pair.m_concurrentTaskScheduler.m_tasks; } }

			/// <summary>Gets the number of tasks currently being executed.</summary>
			public int CurrentlyExecutingTaskCount
			{
				get { return (m_pair.m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL) ? 1 : m_pair.m_processingCount; }
			}

			/// <summary>Gets the underlying task scheduler that actually executes the tasks.</summary>
			public TaskScheduler TargetScheduler { get { return m_pair.m_underlyingTaskScheduler; } }
		}

		/// <summary>Gets an enumeration for debugging that represents the current state of the scheduler pair.</summary>
		/// <remarks>This is only for debugging.  It does not take the necessary locks to be useful for runtime usage.</remarks>
		private ProcessingMode ModeForDebugger
		{
			get
			{
				// If our completion task is done, so are we.
				if (m_completionState != null && m_completionState.Task.IsCompleted) return ProcessingMode.Completed;

				// Otherwise, summarize our current state.
				var mode = ProcessingMode.NotCurrentlyProcessing;
				if (m_processingCount == EXCLUSIVE_PROCESSING_SENTINEL) mode |= ProcessingMode.ProcessingExclusiveTask;
				if (m_processingCount >= 1) mode |= ProcessingMode.ProcessingConcurrentTasks;
				if (CompletionRequested) mode |= ProcessingMode.Completing;
				return mode;
			}
		}

		/// <summary>Asserts that a given synchronization object is either held or not held.</summary>
		/// <param name="syncObj">The monitor to check.</param>
		/// <param name="held">Whether we want to assert that it's currently held or not held.</param>
		[Conditional("DEBUG")]
		public static void ContractAssertMonitorStatus(object syncObj, bool held)
		{
			Contract.Requires(syncObj != null, "The monitor object to check must be provided.");
#if PRENET45
#if DEBUG
			// PRENET45

			if (ShouldCheckMonitorStatus)
			{
				bool exceptionThrown;
				try
				{
					Monitor.Pulse(syncObj); // throws a SynchronizationLockException if the monitor isn't held by this thread
					exceptionThrown = false;
				}
				catch (SynchronizationLockException) { exceptionThrown = true; }
				Contract.Assert(held == !exceptionThrown, "The locking scheme was not correctly followed.");
			}
#endif
#else
			Contract.Assert(Monitor.IsEntered(syncObj) == held, "The locking scheme was not correctly followed.");
#endif
		}

		/// <summary>Gets the options to use for tasks.</summary>
		/// <param name="isReplacementReplica">If this task is being created to replace another.</param>
		/// <remarks>
		/// These options should be used for all tasks that have the potential to run user code or
		/// that are repeatedly spawned and thus need a modicum of fair treatment.
		/// </remarks>
		/// <returns>The options to use.</returns>
		public static TaskCreationOptions GetCreationOptionsForTask(bool isReplacementReplica = false)
		{
			TaskCreationOptions options =
#if PRENET45
				TaskCreationOptions.None;
#else
				TaskCreationOptions.DenyChildAttach;
#endif
			if (isReplacementReplica) options |= TaskCreationOptions.PreferFairness;
			return options;
		}

		/// <summary>Provides an enumeration that represents the current state of the scheduler pair.</summary>
		[Flags]
		private enum ProcessingMode : byte
		{
			/// <summary>The scheduler pair is currently dormant, with no work scheduled.</summary>
			NotCurrentlyProcessing = 0x0,
			/// <summary>The scheduler pair has queued processing for exclusive tasks.</summary>
			ProcessingExclusiveTask = 0x1,
			/// <summary>The scheduler pair has queued processing for concurrent tasks.</summary>
			ProcessingConcurrentTasks = 0x2,
			/// <summary>Completion has been requested.</summary>
			Completing = 0x4,
			/// <summary>The scheduler pair is finished processing.</summary>
			Completed = 0x8
		}

		// ==++==
		//
		//   Copyright (c) Microsoft Corporation.  All rights reserved.
		//
		// ==--==
		// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
		//
		// ProducerConsumerQueues.cs
		//
		// <OWNER>Microsoft, Microsoft</OWNER>
		//
		// Specialized producer/consumer queues.
		//
		//
		// ************<IMPORTANT NOTE>*************
		//
		// There are two exact copies of this file:
		//  src\ndp\clr\src\bcl\system\threading\tasks\producerConsumerQueue.cs
		//  src\ndp\fx\src\dataflow\system\threading\tasks\dataflow\internal\producerConsumerQueue.cs
		// Keep both of them consistent by changing the other file when you change this one, also avoid:
		//  1- To reference interneal types in mscorlib
		//  2- To reference any dataflow specific types
		// This should be fixed post Dev11 when this class becomes public.
		//
		// ************</IMPORTANT NOTE>*************
		// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

		/// <summary>Represents a producer/consumer queue used internally by dataflow blocks.</summary>
		/// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
		private interface IProducerConsumerQueue<T> : IEnumerable<T>
		{
			/// <summary>Enqueues an item into the queue.</summary>
			/// <param name="item">The item to enqueue.</param>
			/// <remarks>This method is meant to be thread-safe subject to the particular nature of the implementation.</remarks>
			void Enqueue(T item);

			/// <summary>Attempts to dequeue an item from the queue.</summary>
			/// <param name="result">The dequeued item.</param>
			/// <returns>true if an item could be dequeued; otherwise, false.</returns>
			/// <remarks>This method is meant to be thread-safe subject to the particular nature of the implementation.</remarks>
			bool TryDequeue(out T result);

			/// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
			/// <param name="syncObj">The sync object used to lock</param>
			/// <returns>The collection count</returns>
			int GetCountSafe(object syncObj);

			/// <summary>Gets whether the collection is currently empty.</summary>
			/// <remarks>This method may or may not be thread-safe.</remarks>
			bool IsEmpty { get; }

			/// <summary>Gets the number of items in the collection.</summary>
			/// <remarks>In many implementations, this method will not be thread-safe.</remarks>
			int Count { get; }
		}

		/// <summary>
		/// Provides a producer/consumer queue safe to be used by any number of producers and consumers concurrently.
		/// </summary>
		/// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
		[DebuggerDisplay("Count = {Count}")]
		private sealed class MultiProducerMultiConsumerQueue<T> : ConcurrentQueue<T>, IProducerConsumerQueue<T>
		{
			/// <summary>Enqueues an item into the queue.</summary>
			/// <param name="item">The item to enqueue.</param>
			void IProducerConsumerQueue<T>.Enqueue(T item) { base.Enqueue(item); }

			/// <summary>Attempts to dequeue an item from the queue.</summary>
			/// <param name="result">The dequeued item.</param>
			/// <returns>true if an item could be dequeued; otherwise, false.</returns>
			bool IProducerConsumerQueue<T>.TryDequeue(out T result) { return base.TryDequeue(out result); }

			/// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
			/// <remarks>ConcurrentQueue.Count is thread safe, no need to acquire the lock.</remarks>
			int IProducerConsumerQueue<T>.GetCountSafe(object syncObj) { return base.Count; }

			/// <summary>Gets whether the collection is currently empty.</summary>
			bool IProducerConsumerQueue<T>.IsEmpty { get { return base.IsEmpty; } }

			/// <summary>Gets the number of items in the collection.</summary>
			int IProducerConsumerQueue<T>.Count { get { return base.Count; } }
		}

		/// <summary>
		/// Provides a producer/consumer queue safe to be used by only one producer and one consumer concurrently.
		/// </summary>
		/// <typeparam name="T">Specifies the type of data contained in the queue.</typeparam>
		[DebuggerDisplay("Count = {Count}")]
		[DebuggerTypeProxy(typeof(SingleProducerSingleConsumerQueue<>.SingleProducerSingleConsumerQueue_DebugView))]
		private sealed class SingleProducerSingleConsumerQueue<T> : IProducerConsumerQueue<T>
		{
			// Design:
			//
			// SingleProducerSingleConsumerQueue (SPSCQueue) is a concurrent queue designed to be used
			// by one producer thread and one consumer thread. SPSCQueue does not work correctly when used by
			// multiple producer threads concurrently or multiple consumer threads concurrently.
			//
			// SPSCQueue is based on segments that behave like circular buffers. Each circular buffer is represented
			// as an array with two indexes: m_first and m_last. m_first is the index of the array slot for the consumer
			// to read next, and m_last is the slot for the producer to write next. The circular buffer is empty when
			// (m_first == m_last), and full when ((m_last+1) % m_array.Length == m_first).
			//
			// Since m_first is only ever modified by the consumer thread and m_last by the producer, the two indices can
			// be updated without interlocked operations. As long as the queue size fits inside a single circular buffer,
			// enqueues and dequeues simply advance the corresponding indices around the circular buffer. If an enqueue finds
			// that there is no room in the existing buffer, however, a new circular buffer is allocated that is twice as big
			// as the old buffer. From then on, the producer will insert values into the new buffer. The consumer will first
			// empty out the old buffer and only then follow the producer into the new (larger) buffer.
			//
			// As described above, the enqueue operation on the fast path only modifies the m_first field of the current segment.
			// However, it also needs to read m_last in order to verify that there is room in the current segment. Similarly, the
			// dequeue operation on the fast path only needs to modify m_last, but also needs to read m_first to verify that the
			// queue is non-empty. This results in true cache line sharing between the producer and the consumer.
			//
			// The cache line sharing issue can be mitigating by having a possibly stale copy of m_first that is owned by the producer,
			// and a possibly stale copy of m_last that is owned by the consumer. So, the consumer state is described using
			// (m_first, m_lastCopy) and the producer state using (m_firstCopy, m_last). The consumer state is separated from
			// the producer state by padding, which allows fast-path enqueues and dequeues from hitting shared cache lines.
			// m_lastCopy is the consumer's copy of m_last. Whenever the consumer can tell that there is room in the buffer
			// simply by observing m_lastCopy, the consumer thread does not need to read m_last and thus encounter a cache miss. Only
			// when the buffer appears to be empty will the consumer refresh m_lastCopy from m_last. m_firstCopy is used by the producer
			// in the same way to avoid reading m_first on the hot path.

			/// <summary>The initial size to use for segments (in number of elements).</summary>
			private const int INIT_SEGMENT_SIZE = 32; // must be a power of 2
			/// <summary>The maximum size to use for segments (in number of elements).</summary>
			private const int MAX_SEGMENT_SIZE = 0x1000000; // this could be made as large as Int32.MaxValue / 2

			/// <summary>The head of the linked list of segments.</summary>
			private volatile Segment m_head;
			/// <summary>The tail of the linked list of segments.</summary>
			private volatile Segment m_tail;

			/// <summary>Initializes the queue.</summary>
			public SingleProducerSingleConsumerQueue()
			{
				// Validate constants in ctor rather than in an explicit cctor that would cause perf degradation
				Contract.Assert(INIT_SEGMENT_SIZE > 0, "Initial segment size must be > 0.");
				Contract.Assert((INIT_SEGMENT_SIZE & (INIT_SEGMENT_SIZE - 1)) == 0, "Initial segment size must be a power of 2");
				Contract.Assert(INIT_SEGMENT_SIZE <= MAX_SEGMENT_SIZE, "Initial segment size should be <= maximum.");
				Contract.Assert(MAX_SEGMENT_SIZE < Int32.MaxValue / 2, "Max segment size * 2 must be < Int32.MaxValue, or else overflow could occur.");

				// Initialize the queue
				m_head = m_tail = new Segment(INIT_SEGMENT_SIZE);
			}

			/// <summary>Enqueues an item into the queue.</summary>
			/// <param name="item">The item to enqueue.</param>
			public void Enqueue(T item)
			{
				Segment segment = m_tail;
				var array = segment.m_array;
				int last = segment.m_state.m_last; // local copy to avoid multiple volatile reads

				// Fast path: there's obviously room in the current segment
				int tail2 = (last + 1) & (array.Length - 1);
				if (tail2 != segment.m_state.m_firstCopy)
				{
					array[last] = item;
					segment.m_state.m_last = tail2;
				}
				// Slow path: there may not be room in the current segment.
				else EnqueueSlow(item, ref segment);
			}

			/// <summary>Attempts to dequeue an item from the queue.</summary>
			/// <param name="result">The dequeued item.</param>
			/// <returns>true if an item could be dequeued; otherwise, false.</returns>
			public bool TryDequeue(out T result)
			{
				Segment segment = m_head;
				var array = segment.m_array;
				int first = segment.m_state.m_first; // local copy to avoid multiple volatile reads

				// Fast path: there's obviously data available in the current segment
				if (first != segment.m_state.m_lastCopy)
				{
					result = array[first];
					array[first] = default(T); // Clear the slot to release the element
					segment.m_state.m_first = (first + 1) & (array.Length - 1);
					return true;
				}
				// Slow path: there may not be data available in the current segment
				else return TryDequeueSlow(ref segment, ref array, out result);
			}

			/// <summary>Attempts to peek at an item in the queue.</summary>
			/// <param name="result">The peeked item.</param>
			/// <returns>true if an item could be peeked; otherwise, false.</returns>
			public bool TryPeek(out T result)
			{
				Segment segment = m_head;
				var array = segment.m_array;
				int first = segment.m_state.m_first; // local copy to avoid multiple volatile reads

				// Fast path: there's obviously data available in the current segment
				if (first != segment.m_state.m_lastCopy)
				{
					result = array[first];
					return true;
				}
				// Slow path: there may not be data available in the current segment
				else return TryPeekSlow(ref segment, ref array, out result);
			}

			/// <summary>Attempts to dequeue an item from the queue.</summary>
			/// <param name="predicate">The predicate that must return true for the item to be dequeued.  If null, all items implicitly return true.</param>
			/// <param name="result">The dequeued item.</param>
			/// <returns>true if an item could be dequeued; otherwise, false.</returns>
			public bool TryDequeueIf(Predicate<T> predicate, out T result)
			{
				Segment segment = m_head;
				var array = segment.m_array;
				int first = segment.m_state.m_first; // local copy to avoid multiple volatile reads

				// Fast path: there's obviously data available in the current segment
				if (first != segment.m_state.m_lastCopy)
				{
					result = array[first];
					if (predicate == null || predicate(result))
					{
						array[first] = default(T); // Clear the slot to release the element
						segment.m_state.m_first = (first + 1) & (array.Length - 1);
						return true;
					}
					else
					{
						result = default(T);
						return false;
					}
				}
				// Slow path: there may not be data available in the current segment
				else return TryDequeueIfSlow(predicate, ref segment, ref array, out result);
			}

			public void Clear()
			{
				T ignored;
				while (TryDequeue(out ignored)) ;
			}

			/// <summary>Gets an enumerable for the collection.</summary>
			/// <remarks>WARNING: This should only be used for debugging purposes.  It is not safe to be used concurrently.</remarks>
			public IEnumerator<T> GetEnumerator()
			{
				for (Segment segment = m_head; segment != null; segment = segment.m_next)
				{
					for (int pt = segment.m_state.m_first;
						pt != segment.m_state.m_last;
						pt = (pt + 1) & (segment.m_array.Length - 1))
					{
						yield return segment.m_array[pt];
					}
				}
			}

			/// <summary>Enqueues an item into the queue.</summary>
			/// <param name="item">The item to enqueue.</param>
			/// <param name="segment">The segment in which to first attempt to store the item.</param>
			private void EnqueueSlow(T item, ref Segment segment)
			{
				Contract.Requires(segment != null, "Expected a non-null segment.");

				if (segment.m_state.m_firstCopy != segment.m_state.m_first)
				{
					segment.m_state.m_firstCopy = segment.m_state.m_first;
					Enqueue(item); // will only recur once for this enqueue operation
					return;
				}

				int newSegmentSize = m_tail.m_array.Length << 1; // double size
				Contract.Assert(newSegmentSize > 0, "The max size should always be small enough that we don't overflow.");
				if (newSegmentSize > MAX_SEGMENT_SIZE) newSegmentSize = MAX_SEGMENT_SIZE;

				var newSegment = new Segment(newSegmentSize);
				newSegment.m_array[0] = item;
				newSegment.m_state.m_last = 1;
				newSegment.m_state.m_lastCopy = 1;

				try { }
				finally
				{
					// Finally block to protect against corruption due to a thread abort
					// between setting m_next and setting m_tail.
					Volatile.Write(ref m_tail.m_next, newSegment); // ensure segment not published until item is fully stored
					m_tail = newSegment;
				}
			}

			/// <summary>Attempts to dequeue an item from the queue.</summary>
			/// <param name="array">The array from which the item was dequeued.</param>
			/// <param name="segment">The segment from which the item was dequeued.</param>
			/// <param name="result">The dequeued item.</param>
			/// <returns>true if an item could be dequeued; otherwise, false.</returns>
			private bool TryDequeueSlow(ref Segment segment, ref T[] array, out T result)
			{
				Contract.Requires(segment != null, "Expected a non-null segment.");
				Contract.Requires(array != null, "Expected a non-null item array.");

				if (segment.m_state.m_last != segment.m_state.m_lastCopy)
				{
					segment.m_state.m_lastCopy = segment.m_state.m_last;
					return TryDequeue(out result); // will only recur once for this dequeue operation
				}

				if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
				{
					segment = segment.m_next;
					array = segment.m_array;
					m_head = segment;
				}

				var first = segment.m_state.m_first; // local copy to avoid extraneous volatile reads

				if (first == segment.m_state.m_last)
				{
					result = default(T);
					return false;
				}

				result = array[first];
				array[first] = default(T); // Clear the slot to release the element
				segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
				segment.m_state.m_lastCopy = segment.m_state.m_last; // Refresh m_lastCopy to ensure that m_first has not passed m_lastCopy

				return true;
			}

			/// <summary>Attempts to peek at an item in the queue.</summary>
			/// <param name="array">The array from which the item is peeked.</param>
			/// <param name="segment">The segment from which the item is peeked.</param>
			/// <param name="result">The peeked item.</param>
			/// <returns>true if an item could be peeked; otherwise, false.</returns>
			private bool TryPeekSlow(ref Segment segment, ref T[] array, out T result)
			{
				Contract.Requires(segment != null, "Expected a non-null segment.");
				Contract.Requires(array != null, "Expected a non-null item array.");

				if (segment.m_state.m_last != segment.m_state.m_lastCopy)
				{
					segment.m_state.m_lastCopy = segment.m_state.m_last;
					return TryPeek(out result); // will only recur once for this peek operation
				}

				if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
				{
					segment = segment.m_next;
					array = segment.m_array;
					m_head = segment;
				}

				var first = segment.m_state.m_first; // local copy to avoid extraneous volatile reads

				if (first == segment.m_state.m_last)
				{
					result = default(T);
					return false;
				}

				result = array[first];
				return true;
			}

			/// <summary>Attempts to dequeue an item from the queue.</summary>
			/// <param name="predicate">The predicate that must return true for the item to be dequeued.  If null, all items implicitly return true.</param>
			/// <param name="array">The array from which the item was dequeued.</param>
			/// <param name="segment">The segment from which the item was dequeued.</param>
			/// <param name="result">The dequeued item.</param>
			/// <returns>true if an item could be dequeued; otherwise, false.</returns>
			private bool TryDequeueIfSlow(Predicate<T> predicate, ref Segment segment, ref T[] array, out T result)
			{
				Contract.Requires(segment != null, "Expected a non-null segment.");
				Contract.Requires(array != null, "Expected a non-null item array.");

				if (segment.m_state.m_last != segment.m_state.m_lastCopy)
				{
					segment.m_state.m_lastCopy = segment.m_state.m_last;
					return TryDequeueIf(predicate, out result); // will only recur once for this dequeue operation
				}

				if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
				{
					segment = segment.m_next;
					array = segment.m_array;
					m_head = segment;
				}

				var first = segment.m_state.m_first; // local copy to avoid extraneous volatile reads

				if (first == segment.m_state.m_last)
				{
					result = default(T);
					return false;
				}

				result = array[first];
				if (predicate == null || predicate(result))
				{
					array[first] = default(T); // Clear the slot to release the element
					segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
					segment.m_state.m_lastCopy = segment.m_state.m_last; // Refresh m_lastCopy to ensure that m_first has not passed m_lastCopy
					return true;
				}
				else
				{
					result = default(T);
					return false;
				}
			}

			/// <summary>Gets an enumerable for the collection.</summary>
			/// <remarks>WARNING: This should only be used for debugging purposes.  It is not safe to be used concurrently.</remarks>
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

			/// <summary>A thread-safe way to get the number of items in the collection. May synchronize access by locking the provided synchronization object.</summary>
			/// <remarks>The Count is not thread safe, so we need to acquire the lock.</remarks>
			int IProducerConsumerQueue<T>.GetCountSafe(object syncObj)
			{
				Contract.Assert(syncObj != null, "The syncObj parameter is null.");
				lock (syncObj)
				{
					return Count;
				}
			}

			/// <summary>Gets whether the collection is currently empty.</summary>
			/// <remarks>WARNING: This should not be used concurrently without further vetting.</remarks>
			public bool IsEmpty
			{
				// This implementation is optimized for calls from the consumer.
				get
				{
					var head = m_head;
					if (head.m_state.m_first != head.m_state.m_lastCopy) return false; // m_first is volatile, so the read of m_lastCopy cannot get reordered
					if (head.m_state.m_first != head.m_state.m_last) return false;
					return head.m_next == null;
				}
			}

			/// <summary>Gets the number of items in the collection.</summary>
			/// <remarks>WARNING: This should only be used for debugging purposes.  It is not meant to be used concurrently.</remarks>
			public int Count
			{
				get
				{
					int count = 0;
					for (Segment segment = m_head; segment != null; segment = segment.m_next)
					{
						int arraySize = segment.m_array.Length;
						int first, last;
						while (true) // Count is not meant to be used concurrently, but this helps to avoid issues if it is
						{
							first = segment.m_state.m_first;
							last = segment.m_state.m_last;
							if (first == segment.m_state.m_first) break;
						}
						count += (last - first) & (arraySize - 1);
					}
					return count;
				}
			}

			/// <summary>A segment in the queue containing one or more items.</summary>
			[StructLayout(LayoutKind.Sequential)]
			private sealed class Segment
			{
				/// <summary>The data stored in this segment.</summary>
				public readonly T[] m_array;
				/// <summary>The next segment in the linked list of segments.</summary>
				public Segment m_next;
				/// <summary>Details about the segment.</summary>
				public SegmentState m_state; // separated out to enable StructLayout attribute to take effect

				/// <summary>Initializes the segment.</summary>
				/// <param name="size">The size to use for this segment.</param>
				public Segment(int size)
				{
					Contract.Requires((size & (size - 1)) == 0, "Size must be a power of 2");
					m_array = new T[size];
				}
			}

			/// <summary>Stores information about a segment.</summary>
			[StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
			private struct SegmentState
			{
				/// <summary>Padding to reduce false sharing between the segment's array and m_first.</summary>
				public PaddingFor32 m_pad0;

				/// <summary>The index of the current head in the segment.</summary>
				public volatile int m_first;
				/// <summary>A copy of the current tail index.</summary>
				public int m_lastCopy; // not volatile as read and written by the producer, except for IsEmpty, and there m_lastCopy is only read after reading the volatile m_first

				/// <summary>Padding to reduce false sharing between the first and last.</summary>
				public PaddingFor32 m_pad1;

				/// <summary>A copy of the current head index.</summary>
				public int m_firstCopy; // not voliatle as only read and written by the consumer thread
										/// <summary>The index of the current tail in the segment.</summary>
				public volatile int m_last;

				/// <summary>Padding to reduce false sharing with the last and what's after the segment.</summary>
				public PaddingFor32 m_pad2;
			}

			/// <summary>Debugger type proxy for a SingleProducerSingleConsumerQueue of T.</summary>
			private sealed class SingleProducerSingleConsumerQueue_DebugView
			{
				/// <summary>The queue being visualized.</summary>
				private readonly SingleProducerSingleConsumerQueue<T> m_queue;

				/// <summary>Initializes the debug view.</summary>
				public SingleProducerSingleConsumerQueue_DebugView(SingleProducerSingleConsumerQueue<T> queue)
				{
					Contract.Requires(queue != null, "Expected a non-null queue.");
					m_queue = queue;
				}

				/// <summary>Gets the contents of the list.</summary>
				[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
				public T[] Items
				{
					get
					{
						List<T> list = new List<T>();
						foreach (T item in m_queue)
							list.Add(item);
						return list.ToArray();
					}
				}
			}
		}


		/// <summary>A placeholder class for common padding constants and eventually routines.</summary>
		static class PaddingHelpers
		{
			/// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
			public const int CACHE_LINE_SIZE = 128;
		}

		/// <summary>Padding structure used to minimize false sharing in SingleProducerSingleConsumerQueue{T}.</summary>
		[StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE - sizeof(Int32))] // Based on common case of 64-byte cache lines
		struct PaddingFor32
		{
		}
	}
}

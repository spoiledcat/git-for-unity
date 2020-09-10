using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Editor.Tasks
{
	using System;

	public class ThreadSynchronizationContext : SynchronizationContext, IDisposable
	{
		private readonly ConcurrentQueue<PostData> priorityQueue = new ConcurrentQueue<PostData>();
		private readonly ConcurrentQueue<PostData> queue = new ConcurrentQueue<PostData>();
		private readonly ManualResetEventSlim dataSignal = new ManualResetEventSlim(false);
		private readonly CancellationTokenSource cts = new CancellationTokenSource();
		private readonly CancellationTokenSource externalCts;
		private ManualResetEvent completion = new ManualResetEvent(false);

		private int threadId;
		protected bool IsInSyncThread => Thread.CurrentThread.ManagedThreadId == threadId;


		public ThreadSynchronizationContext(CancellationToken token = default)
		{
			externalCts = CancellationTokenSource.CreateLinkedTokenSource(token);
			if (token.CanBeCanceled)
				externalCts.Token.Register(Dispose);
			Task.Factory.StartNew(Start, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
		}

		public void Stop()
		{
			Dispose();
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			if (disposed) return;

			var data = new PostData { Completion = new ManualResetEventSlim(), Callback = d, State = state };
			queue.Enqueue(data);
			lock (dataSignal)
			{
				dataSignal.Set();
			}
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			if (disposed) return;

			if (IsInSyncThread)
			{
				d(state);
			}
			else
			{
				var data = new PostData { Completion = new ManualResetEventSlim(), Callback = d, State = state };
				priorityQueue.Enqueue(data);
				lock (dataSignal)
				{
					dataSignal.Set();
				}
				data.Completion.Wait(cts.Token);
			}
		}

		private void Pump()
		{
			if (disposed) return;

			lock (dataSignal)
			{
				dataSignal.Reset();
			}

			PostData data;
			while (priorityQueue.TryDequeue(out data))
			{
				if (disposed) return;
				data.Run();
			}

			while (queue.TryDequeue(out data))
			{
				if (disposed) return;
				data.Run();
			}
		}

		private void Start()
		{
			try
			{
				SetSynchronizationContext(this);

				threadId = Thread.CurrentThread.ManagedThreadId;

				while (!cts.IsCancellationRequested)
				{
					Pump();
					dataSignal.Wait(cts.Token);
				}
			}
			catch (OperationCanceledException) { }
			finally
			{
				completion.Set();
			}
		}

		private bool disposed;

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;

			if (disposing)
			{
				disposed = true;
				externalCts.Dispose();

				cts.Cancel();

				if (!IsInSyncThread)
					completion.WaitOne();

				try
				{
					cts.Dispose();
					dataSignal.Dispose();
				}
				catch
				{}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		struct PostData
		{
			public ManualResetEventSlim Completion;
			public SendOrPostCallback Callback;
			public object State;

			public void Run()
			{
				if (Completion.IsSet)
					return;

				try
				{
					Callback(State);
				}
				catch { }
				Completion.Set();
			}
		}
	}
}

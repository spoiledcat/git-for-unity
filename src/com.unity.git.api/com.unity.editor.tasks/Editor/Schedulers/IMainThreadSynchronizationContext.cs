namespace Unity.Editor.Tasks
{
	using System;

	public interface IMainThreadSynchronizationContext : IDisposable
	{
		/// <summary>
		/// <summary>Queues a delegate for asynchronous execution.</summary>
		/// </summary>
		/// <param name="action">The delegate to execute.</param>
		void Schedule(Action action);
	}
}

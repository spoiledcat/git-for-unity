using System;

namespace Unity.Editor.Tasks
{
	using System.Diagnostics;
	using System.Threading;

	/// <summary>
	/// A process manager that configures processes for running and keeps track of running processes.
	/// </summary>
	public interface IProcessManager : IDisposable
	{
		/// <summary>
		/// Helper that configures all the necessary parts in order to run a process. This must be called before running
		/// a ProcessTask.
		/// </summary>
		T Configure<T>(T processTask, string workingDirectory = null) where T : IProcessTask;

		/// <summary>
		/// Helper that configures all the necessary parts in order to run a process. This must be called before running
		/// a ProcessTask.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="processTask"></param>
		/// <param name="startInfo"></param>
		/// <returns></returns>
		T Configure<T>(T processTask, ProcessStartInfo startInfo) where T : IProcessTask;

		/// <summary>
		/// Helper that creates a process wrapper for the given process information. This is called by
		/// ProcessTask during the Configure step.
		/// </summary>
		/// <param name="taskName"></param>
		/// <param name="process"></param>
		/// <param name="outputProcessor"></param>
		/// <param name="onStart"></param>
		/// <param name="onEnd"></param>
		/// <param name="onError"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		BaseProcessWrapper WrapProcess(string taskName,
			ProcessStartInfo process,
			IOutputProcessor outputProcessor,
			Action onStart,
			Action onEnd,
			Action<Exception, string> onError,
			CancellationToken token = default);

		/// <summary>
		/// Stops all running processes managed by this manager.
		/// </summary>
		void Stop();

		/// <summary>
		/// Default process environment.
		/// </summary>
		IProcessEnvironment DefaultProcessEnvironment { get; }
	}
}

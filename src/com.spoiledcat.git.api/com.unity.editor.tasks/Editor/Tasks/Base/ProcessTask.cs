// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Unity.Editor.Tasks
{
	using System.Threading.Tasks;
	using Extensions;
	using Unity.Editor.Tasks.Helpers;

	/// <summary>
	/// An external process managed by the <see cref="IProcessManager" /> and
	/// wrapped by a <see cref="IProcessTask" />
	/// </summary>
	public interface IProcess : IDisposable
	{
		/// <summary>
		/// Event raised when the process exits
		/// </summary>
		event Action<IProcess> OnEndProcess;
		/// <summary>
		/// Event raised after the process is finished, with any output that the process sent to stderr.
		/// </summary>
		event Action<string> OnErrorData;
		/// <summary>
		/// Event raised when the process is started.
		/// </summary>
		event Action<IProcess> OnStartProcess;

		/// <summary>
		/// Stops the process.
		/// </summary>
		void Stop();

		/// <summary>
		/// The StandardInput of the process is redirected to this stream writer.
		/// </summary>
		StreamWriter StandardInput { get; }
		/// <summary>
		/// The underlying process id.
		/// </summary>
		int ProcessId { get; }
		/// <summary>
		/// The process name.
		/// </summary>
		string ProcessName { get; }
		/// <summary>
		/// The process arguments.
		/// </summary>
		string ProcessArguments { get; }
		/// <summary>
		/// The underlying process object.
		/// </summary>
		BaseProcessWrapper Wrapper { get; }
	}

	/// <summary>
	/// A task that runs an external process.
	/// </summary>
	public interface IProcessTask : ITask, IProcess
	{
		/// <summary>
		/// The environment for the process. This is a wrapper of <see cref="IEnvironment" /> that also includes a working directory,
		/// and configures environment variables of the process.
		/// </summary>
		IProcessEnvironment ProcessEnvironment { get; }

		/// <summary>
		/// Configures the underlying process object.
		/// </summary>
		void Configure(IProcessManager processManager, ProcessStartInfo startInfo);

		/// <summary>
		/// An overloaded <see cref="ITask.Start()" /> method that returns IProcessTask, to make it easier to chain.
		/// </summary>
		/// <returns>The started task.</returns>
		new IProcessTask Start();

		/// <summary>
		/// An overloaded <see cref="ITask.Start(System.Threading.Tasks.TaskScheduler)" /> method that returns IProcessTask, to make it easier to chain.
		/// </summary>
		/// <returns>The started task.</returns>
		new IProcessTask Start(TaskScheduler customScheduler);

		/// <summary>
		/// If you call this on a running process task, it will trigger the task to finish, raising
		/// OnEnd and OnEndProcess, without stopping the underlying process. Process manager won't
		/// stop a released process on shutdown. This will effectively leak the process, but if you
		/// need to run a background process that won't be stopped if the domain goes down, call this.
		/// </summary>
		void Detach();
	}

	/// <summary>
	/// A task that runs an external process and returns the process output.
	/// </summary>
	/// <typeparam name="T">The output of the process, processed via an IOutputProcessor.</typeparam>
	public interface IProcessTask<T> : ITask<T>, IProcessTask
	{
		/// <summary>
		/// Set the underlying process object,
		/// and optionally sets an output processor, if one wasn't set in the constructor or set in some
		/// other way. The process manager is responsible for creating and configuring the process object.
		/// </summary>
		/// <param name="processManager"></param>
		/// <param name="startInfo"></param>
		/// <param name="processor">The output processor to use to process the process output.</param>
		void Configure(IProcessManager processManager, ProcessStartInfo startInfo, IOutputProcessor<T> processor = null);

		/// <summary>
		/// An overloaded <see cref="ITask.Start()" /> method that returns IProcessTask, to make it easier to chain.
		/// </summary>
		/// <returns>The started task.</returns>
		new IProcessTask<T> Start();

		/// <summary>
		/// An overloaded <see cref="ITask.Start(TaskScheduler)" /> method that returns IProcessTask, to make it easier to chain.
		/// </summary>
		/// <returns>The started task.</returns>
		new IProcessTask<T> Start(TaskScheduler customScheduler);

		/// <inheritdoc />
		event Action<T> OnOutput;
	}


	/// <summary>
	/// A task that runs an external process and returns the process output, converting it in the process to
	/// a different type. This is mainly for creating lists of data, where <typeparamref name="TData"/> is
	/// the type of a single item, and <typeparamref name="T" /> is a List&lt;TData&gt;.
	/// The base <see cref="ITask&lt;TData, T&gt;" /> provides a <see cref="ITask&lt;TData, T&gt;.OnData" />
	/// event that is called whenever the output processor raised the OnEntry event.
	/// </summary>
	/// <typeparam name="TData"></typeparam>
	/// <typeparam name="T"></typeparam>
	public interface IProcessTask<TData, T> : ITask<TData, T>, IProcessTask
	{
		/// <summary>
		/// Set the underlying process object,
		/// and optionally sets an output processor, if one wasn't set in the constructor or set in some
		/// other way. The process manager is responsible for creating and configuring the process object.
		/// </summary>
		/// <param name="processManager"></param>
		/// <param name="startInfo"></param>
		/// <param name="processor">The output processor to use to process the process output.</param>
		void Configure(IProcessManager processManager, ProcessStartInfo startInfo, IOutputProcessor<TData, T> processor = null);

		/// <summary>
		/// An overloaded <see cref="ITask.Start()" /> method that returns IProcessTask, to make it easier to chain.
		/// </summary>
		/// <returns>The started task.</returns>
		new IProcessTask<TData, T> Start();
		/// <summary>
		/// An overloaded <see cref="ITask.Start(TaskScheduler)" /> method that returns IProcessTask, to make it easier to chain.
		/// </summary>
		/// <returns>The started task.</returns>
		new IProcessTask<TData, T> Start(TaskScheduler customScheduler);
	}


	/// <summary>
	/// A task that runs an external process and returns the process output.
	/// </summary>
	/// <typeparam name="T">The output of the process, processed via an IOutputProcessor.</typeparam>
	public class ProcessTask<T> : TaskBase<T>, IProcessTask<T>
	{
		private Exception thrownException = null;
		private T result;

		/// <inheritdoc />
		public event Action<IProcess> OnEndProcess;
		/// <inheritdoc />
		public event Action<string> OnErrorData;
		/// <inheritdoc />
		public event Action<IProcess> OnStartProcess;
		/// <inheritdoc />
		public event Action<T> OnOutput;

		/// <summary>
		/// Runs a Process with the passed arguments
		/// </summary>
		public ProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null,
			IOutputProcessor<T> outputProcessor = null,
			CancellationToken token = default
		)
			: base(taskManager, token)
		{
			OutputProcessor = outputProcessor;
			ProcessEnvironment = processEnvironment;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		/// <summary>
		/// Set the underlying process object,
		/// and optionally sets an output processor, if one wasn't set in the constructor or set in some
		/// other way. The process manager is responsible for creating and configuring the process object via
		/// a call to <see cref="IProcessManager.WrapProcess" />.
		/// </summary>
		public virtual void Configure(IProcessManager processManager, ProcessStartInfo startInfo, IOutputProcessor<T> processor = null)
		{
			ProcessName = startInfo.FileName;
			ProcessArguments = startInfo.Arguments;

			OutputProcessor = processor ?? OutputProcessor;
			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			ProcessEnvironment.Configure(startInfo);

			Wrapper = processManager.WrapProcess(Name, startInfo, OutputProcessor,
				RaiseOnStartProcess,
				HandleOnEndProcess,
				(ex, error) => {
					thrownException = ex;
					Errors = error;
				},
				Token);
		}

		/// <inheritdoc />
		void IProcessTask.Configure(IProcessManager processManager, ProcessStartInfo process) => Configure(processManager, process, null);

		/// <inheritdoc />
		public new IProcessTask<T> Start()
		{
			base.Start();
			return this;
		}

		/// <inheritdoc />
		public new IProcessTask<T> Start(TaskScheduler customScheduler)
		{
			base.Start(customScheduler);
			return this;
		}

		/// <inheritdoc />
		IProcessTask IProcessTask.Start() => Start();
		/// <inheritdoc />
		IProcessTask IProcessTask.Start(TaskScheduler customScheduler) => Start(customScheduler);

		/// <inheritdoc />
		public void Stop()
		{
			Wrapper?.Stop();
		}

		public virtual void Detach()
		{
			Wrapper?.Detach();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
		}

		protected override void RaiseOnEnd(T data)
		{
			base.RaiseOnEnd(data);
			OnStartProcess = OnEndProcess = null;
		}

		/// <summary>
		/// Called when the process has been started.
		/// </summary>
		protected virtual void RaiseOnStartProcess()
		{
			OnStartProcess?.Invoke(this);
		}

		/// <summary>
		/// Call after OnEnd, when the process has finished.
		/// </summary>
		protected virtual void RaiseOnEndProcess()
		{
			OnEndProcess?.Invoke(this);
		}

		/// <inheritdoc />
		protected virtual void ConfigureOutputProcessor()
		{
			if (OutputProcessor == null && (typeof(T) != typeof(string)))
			{
				throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
			}
			OutputProcessor.OnEntry += RaiseOnOutput;
		}

		protected void RaiseOnOutput(T data)
		{
			OnOutput?.Invoke(data);
		}


		/// <inheritdoc />
		protected override T RunWithReturn(bool success)
		{
			result = base.RunWithReturn(success);
			Wrapper.Run();
			return result;
		}

		private void HandleOnEndProcess()
		{
			try
			{
				if (OutputProcessor != null)
					result = OutputProcessor.Result;

				if (typeof(T) == typeof(string) && result == null && !Wrapper.StartInfo.CreateNoWindow)
					result = (T)(object)"Process running";

				if (!String.IsNullOrEmpty(Errors))
					RaiseOnErrorData();
			}
			catch (Exception ex)
			{
				if (thrownException == null)
					thrownException = new ProcessException(ex.Message, ex);
				else
					thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
			}

			if (thrownException != null && !RaiseFaultHandlers(thrownException))
			{
				RaiseOnEndProcess();
				Exception.Rethrow();
			}
			RaiseOnEndProcess();
		}

		protected virtual void RaiseOnErrorData()
		{
			OnErrorData?.Invoke(Errors);
		}

		private bool disposed;

		/// <inheritdoc />
		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			disposed = true;
			if (disposing)
			{
				OnStartProcess = OnEndProcess = null;
				Wrapper?.Dispose();
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public BaseProcessWrapper Wrapper { get; private set; }
		/// <inheritdoc />
		public IProcessEnvironment ProcessEnvironment { get; private set; }
		/// <inheritdoc />
		/// <inheritdoc />
		public int ProcessId => Wrapper.ProcessId;
		/// <inheritdoc />
		public override bool Successful => base.Successful && Wrapper.ExitCode == 0;
		/// <inheritdoc />
		public StreamWriter StandardInput => Wrapper?.Input;
		/// <inheritdoc />
		public virtual string ProcessName { get; protected set; }
		/// <inheritdoc />
		public virtual string ProcessArguments { get; protected set; }

		/// <inheritdoc />
		protected IOutputProcessor<T> OutputProcessor { get; set; }
	}

	/// <summary>
	/// A helper process task that returns a list of data from the output of the process.
	/// </summary>
	/// <typeparam name="T">The type of the items on the returned list.</typeparam>
	public class ProcessTaskWithListOutput<T> : DataTaskBase<T, List<T>>, IProcessTask<T, List<T>>, IDisposable
	{
		private Exception thrownException = null;
		private List<T> result;

		/// <inheritdoc />
		public event Action<IProcess> OnEndProcess;
		/// <inheritdoc />
		public event Action<string> OnErrorData;
		/// <inheritdoc />
		public event Action<IProcess> OnStartProcess;
		/// <inheritdoc />
		public event Action<T> OnOutput;

		/// <summary>
		/// Runs a Process with the passed arguments
		/// </summary>
		/// <param name="taskManager"></param>
		/// <param name="token"></param>
		/// <param name="processEnvironment"></param>
		/// <param name="executable"></param>
		/// <param name="arguments"></param>
		/// <param name="outputProcessor"></param>
		public ProcessTaskWithListOutput(
			ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable = null,
			string arguments = null,
			IOutputProcessor<T, List<T>> outputProcessor = null,
			CancellationToken token = default)
			: base(taskManager, token)
		{
			this.OutputProcessor = outputProcessor;
			ProcessEnvironment = processEnvironment;
			ProcessArguments = arguments;
			ProcessName = executable;
		}

		/// <summary>
		/// Set the underlying process object,
		/// and optionally sets an output processor, if one wasn't set in the constructor or set in some
		/// other way. The process manager is responsible for creating and configuring the process object via
		/// a call to <see cref="IProcessManager.WrapProcess" />.
		/// </summary>
		public virtual void Configure(IProcessManager processManager, ProcessStartInfo startInfo, IOutputProcessor<T, List<T>> processor = null)
		{
			OutputProcessor = processor ?? OutputProcessor;
			ConfigureOutputProcessor();

			this.EnsureNotNull(OutputProcessor, nameof(OutputProcessor));

			ProcessEnvironment.Configure(startInfo);

			Wrapper = processManager.WrapProcess(Name, startInfo, OutputProcessor,
				RaiseOnStartProcess,
				HandleOnEndProcess,
				(ex, error) => {
					thrownException = ex;
					Errors = error;
				},
				Token);
		}

		/// <inheritdoc />
		void IProcessTask.Configure(IProcessManager processManager, ProcessStartInfo process) => Configure(processManager, process, null);

		/// <inheritdoc />
		public new IProcessTask<T, List<T>> Start()
		{
			base.Start();
			return this;
		}

		/// <inheritdoc />
		public new IProcessTask<T, List<T>> Start(TaskScheduler customScheduler)
		{
			base.Start(customScheduler);
			return this;
		}

		IProcessTask IProcessTask.Start() => Start();
		IProcessTask IProcessTask.Start(TaskScheduler customScheduler) => Start(customScheduler);


		/// <inheritdoc />
		public void Stop()
		{
			Wrapper?.Stop();
		}

		/// <inheritdoc />
		public virtual void Detach()
		{
			Wrapper?.Detach();
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{Task?.Id ?? -1} {Name} {GetType()} {ProcessName} {ProcessArguments}";
		}

		/// <summary>
		/// Called when the process has been started.
		/// </summary>
		protected virtual void RaiseOnStartProcess()
		{
			OnStartProcess?.Invoke(this);
		}

		/// <summary>
		/// Call after OnEnd, when the process has finished.
		/// </summary>
		protected virtual void RaiseOnEndProcess()
		{
			OnEndProcess?.Invoke(this);
		}

		/// <inheritdoc />
		protected virtual void ConfigureOutputProcessor()
		{
			if (OutputProcessor == null && (typeof(T) != typeof(string)))
			{
				throw new InvalidOperationException("ProcessTask without an output processor must be defined as IProcessTask<string>");
			}
			OutputProcessor.OnEntry += RaiseOnOutput;
		}

		protected void RaiseOnOutput(T data)
		{
			RaiseOnData(data);
			OnOutput?.Invoke(data);
		}

		/// <inheritdoc />
		protected override List<T> RunWithReturn(bool success)
		{
			result = base.RunWithReturn(success);
			Wrapper.Run();
			return result;
		}

		private void HandleOnEndProcess()
		{
			try
			{
				if (OutputProcessor != null)
					result = OutputProcessor.Result;
				if (result == null)
					result = new List<T>();

				if (!string.IsNullOrEmpty(Errors))
					OnErrorData?.Invoke(Errors);
			}
			catch (Exception ex)
			{
				if (thrownException == null)
					thrownException = new ProcessException(ex.Message, ex);
				else
					thrownException = new ProcessException(thrownException.GetExceptionMessage(), ex);
			}

			if (thrownException != null && !RaiseFaultHandlers(thrownException))
			{
				RaiseOnEndProcess();
				Exception.Rethrow();
			}
			RaiseOnEndProcess();
		}

		private bool disposed;

		/// <inheritdoc />
		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;
			if (disposing)
			{
				Wrapper?.Dispose();
				disposed = true;
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}


		public BaseProcessWrapper Wrapper { get; private set; }
		/// <inheritdoc />
		public IProcessEnvironment ProcessEnvironment { get; private set; }
		/// <inheritdoc />
		/// <inheritdoc />
		public int ProcessId => Wrapper.ProcessId;
		/// <inheritdoc />
		public override bool Successful => base.Successful && Wrapper.ExitCode == 0;
		/// <inheritdoc />
		public StreamWriter StandardInput => Wrapper?.Input;
		/// <inheritdoc />
		public virtual string ProcessName { get; protected set; }
		/// <inheritdoc />
		public virtual string ProcessArguments { get; protected set; }
		/// <inheritdoc />
		protected IOutputProcessor<T, List<T>> OutputProcessor { get; set; }
	}
}

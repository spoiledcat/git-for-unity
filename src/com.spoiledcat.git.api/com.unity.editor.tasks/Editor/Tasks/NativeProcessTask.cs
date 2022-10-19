// Copyright 2019 Unity
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace Unity.Editor.Tasks
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Helpers;

	/// <summary>
	/// Runs a process.
	/// </summary>
	public class NativeProcessTask : BaseProcessTask
	{
		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
				IEnvironment environment,
				string executable, string arguments,
				CancellationToken token = default)
			: base(taskManager, null, new ProcessEnvironment(environment), null,
				  executable, arguments, null, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
				IProcessEnvironment processEnvironment,
				string executable, string arguments,
				CancellationToken token = default)
			: base(taskManager, null, processEnvironment, null,
				  executable, arguments, null, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager, IProcessManager processManager,
				string executable, string arguments,
				string workingDirectory = null,
				CancellationToken token = default)
			: base(taskManager, processManager,
				  processManager.DefaultProcessEnvironment,
				  null,
				  executable, arguments, workingDirectory, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager, IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable, string arguments,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager,
					processEnvironment, null,
					executable, arguments, workingDirectory, false, true, token)
		{ }
	}

	/// <summary>
	/// Runs a process.
	/// </summary>
	public class NativeProcessTask<T> : BaseProcessTask<T>
	{
		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, null,
				  executable, arguments, null, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), null,
				  executable, arguments, null, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), null,
				  executable, arguments, null, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), null,
				  executable, arguments, null, null, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, null,
				  executable, arguments, null, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, null,
				  executable, arguments, null, null, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, null, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, null, processor, false, true, token)
		{ }
	}

	/// <summary>
	/// Runs a process.
	/// </summary>
	public class NativeProcessListTask<T> : BaseProcessListTask<T>
	{
		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, null,
				  executable, arguments, null, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), null,
				  executable, arguments, null, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, outputProcessor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), null,
				  executable, arguments, null, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), null,
				  executable, arguments, null, null, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, null,
				  executable, arguments, null, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, null,
				  executable, arguments, null, null, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, null, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, isMatch, processor, false, true, token)
		{ }

		/// <summary>
		/// Runs a process.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public NativeProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, null,
				  executable, arguments, workingDirectory, null, processor, false, true, token)
		{ }
	}
}

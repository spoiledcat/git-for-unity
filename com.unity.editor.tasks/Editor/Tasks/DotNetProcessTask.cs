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
	using Internal.IO;

	/// <summary>
	/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
	/// it runs the executable using Unity's mono.
	/// </summary>
	public class DotNetProcessTask : BaseProcessTask
	{
		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
				IEnvironment environment,
				string executable, string arguments,
				CancellationToken token = default)
			: base(taskManager, null, new ProcessEnvironment(environment), environment,
				  executable, arguments, null, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
				IProcessEnvironment processEnvironment,
				IEnvironment environment,
				string executable, string arguments,
				CancellationToken token = default)
			: base(taskManager, null, processEnvironment, environment,
				  executable, arguments, null, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager, IProcessManager processManager,
				string executable, string arguments,
				string workingDirectory = null,
				CancellationToken token = default)
			: base(taskManager, processManager,
				  processManager.DefaultProcessEnvironment,
				  processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager, IProcessManager processManager,
			IEnvironment environment, IProcessEnvironment processEnvironment,
			string executable, string arguments,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager,
					processEnvironment,environment,
					executable, arguments, workingDirectory, false, false, token)
		{ }
	}

	/// <summary>
	/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
	/// it runs the executable using Unity's mono.
	/// </summary>
	public class DotNetProcessTask<T> : BaseProcessTask<T>
	{
		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, environment,
				  executable, arguments, null, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), environment,
				  executable, arguments, null, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), environment,
				  executable, arguments, null, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), environment,
				  executable, arguments, null, null, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, environment,
				  executable, arguments, null, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, environment,
				  executable, arguments, null, null, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, null, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, bool> isMatch,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, null, processor, false, false, token)
		{ }
	}

	/// <summary>
	/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
	/// it runs the executable using Unity's mono.
	/// </summary>
	public class DotNetProcessListTask<T> : BaseProcessListTask<T>
	{
		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, environment,
				  executable, arguments, null, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), environment,
				  executable, arguments, null, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			IOutputProcessor<T, List<T>> outputProcessor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, outputProcessor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), environment,
				  executable, arguments, null, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  new ProcessEnvironment(environment), environment,
				  executable, arguments, null, null, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, environment,
				  executable, arguments, null, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task before running it.</remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			CancellationToken token = default)
			: base(taskManager, null,
				  processEnvironment, environment,
				  executable, arguments, null, null, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processManager.DefaultProcessEnvironment, processManager.DefaultProcessEnvironment.Environment,
				  executable, arguments, workingDirectory, null, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, bool> isMatch,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, isMatch, processor, false, false, token)
		{ }

		/// <summary>
		/// Runs a dotnet process. On Windows, it just runs the executable. On non-Windows,
		/// it runs the executable using Unity's mono.
		/// </summary>
		/// <remarks>You don't need to call <see cref="ProcessManager.Configure{T}(T, string)"/> on this task,
		/// it already does it in the constructor.
		/// </remarks>
		public DotNetProcessListTask(ITaskManager taskManager,
			IProcessManager processManager,
			IProcessEnvironment processEnvironment,
			IEnvironment environment,
			string executable,
			string arguments,
			Func<IProcessTask<T, List<T>>, string, T> processor,
			string workingDirectory = null,
			CancellationToken token = default)
			: base(taskManager, processManager.EnsureNotNull(nameof(processManager)),
				  processEnvironment ?? processManager.DefaultProcessEnvironment, environment,
				  executable, arguments, workingDirectory, null, processor, false, false, token)
		{ }
	}
}

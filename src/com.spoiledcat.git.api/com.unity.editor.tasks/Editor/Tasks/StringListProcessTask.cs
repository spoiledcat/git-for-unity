namespace Unity.Editor.Tasks
{
	using System.Threading;
	using Helpers;

	public class StringListProcessTask : ProcessTaskWithListOutput<string>
	{
		public StringListProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments, string workingDirectory = null,
			CancellationToken token = default
		)
			: base(taskManager,
				processManager.EnsureNotNull(nameof(processManager)).DefaultProcessEnvironment,
				executable, arguments,
				new StringListOutputProcessor(), token: token)
		{
			processManager.Configure(this, workingDirectory);
		}

		public override TaskAffinity Affinity { get; set; } = TaskAffinity.None;
	}

}

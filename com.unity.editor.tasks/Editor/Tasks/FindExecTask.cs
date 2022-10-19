namespace Unity.Editor.Tasks
{
	using Helpers;

	public class FindExecTask : NativeProcessTask<string>
	{
		public FindExecTask(ITaskManager taskManager, IProcessManager processManager, string execToFind)
			: base(taskManager, processManager,
				Guard.EnsureNotNull(processManager, nameof(processManager)).DefaultProcessEnvironment.Environment.IsWindows ? "where" : "which",
				execToFind, new FirstNonNullOutputProcessor<string>())
		{
			Affinity = TaskAffinity.None;
		}

		public override TaskAffinity Affinity => TaskAffinity.None;
	}
}

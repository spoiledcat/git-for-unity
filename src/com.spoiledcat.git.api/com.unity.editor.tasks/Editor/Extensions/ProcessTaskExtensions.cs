namespace Unity.Editor.Tasks
{
	using System.Diagnostics;

	public static class ProcessTaskExtensions
	{
		/// <summary>
		/// Helper that calls processManager.Configure
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="task"></param>
		/// <param name="processManager"></param>
		/// <param name="workingDirectory"></param>
		/// <returns></returns>
		public static T Configure<T>(this T task, IProcessManager processManager, string workingDirectory = null)
			where T : IProcessTask => processManager.Configure(task, workingDirectory);
	}
}

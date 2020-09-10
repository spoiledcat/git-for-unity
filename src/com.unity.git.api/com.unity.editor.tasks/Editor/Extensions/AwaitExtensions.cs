namespace Unity.Editor.Tasks.Extensions
{
	using System.Threading.Tasks;

	public static class AwaitExtensions
	{
		public static IAwaitable AwaitInline(this Task task) => new ThreadingHelper.AwaitableWrapper(new ThreadingHelper.ExecuteContinuationSynchronouslyAwaiter(task));

		public static IAwaitable AwaitOn(this Task task, ITaskManager taskManager, TaskAffinity affinity) => new ThreadingHelper.AwaitableWrapper(taskManager.GetScheduler(affinity));

		public static IAwaitable SwitchTo(this TaskScheduler scheduler) => new ThreadingHelper.AwaitableWrapper(scheduler);
	}
}

using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitCountObjectsTask : NativeProcessTask<int>
    {
        private const string TaskName = "git count-objects";

        public GitCountObjectsTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new GitCountObjectsProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return "count-objects"; }
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Counting git objects...";
    }
}

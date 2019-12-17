using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLfsVersionTask : NativeProcessTask<TheVersion>
    {
        private const string TaskName = "git lfs version";

        public GitLfsVersionTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new LfsVersionOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "lfs version"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
        public override string Message { get; set; } = "Reading LFS version...";
    }
}

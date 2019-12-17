using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLfsInstallTask : NativeProcessTask<string>
    {
        private const string TaskName = "git lsf install";

        public GitLfsInstallTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "lfs install"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Initializing LFS...";
    }
}

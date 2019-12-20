using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLfsVersionTask : GitProcessTask<TheVersion>
    {
        private const string TaskName = "git lfs version";

        public GitLfsVersionTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            string gitLfsExecutablePath, CancellationToken token = default)
            : base(taskManager, processEnvironment, gitLfsExecutablePath, "version",
                outputProcessor: new LfsVersionOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public GitLfsVersionTask(IPlatform platform, CancellationToken token = default)
            : base(platform, "lfs version",
                outputProcessor: new LfsVersionOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Reading git-lfs version...";
    }
}

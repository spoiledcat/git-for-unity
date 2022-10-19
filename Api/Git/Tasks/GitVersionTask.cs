using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitVersionTask : GitProcessTask<TheVersion>
    {
        private const string TaskName = "git --version";
        private const string Arguments = "--version";

        public GitVersionTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            string gitExecutablePath, CancellationToken token = default)
            : base(taskManager, processEnvironment, gitExecutablePath, Arguments,
                outputProcessor: new VersionOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public GitVersionTask(IPlatform platform, CancellationToken token = default)
            : base(platform, Arguments, outputProcessor: new VersionOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Reading git version...";
    }
}

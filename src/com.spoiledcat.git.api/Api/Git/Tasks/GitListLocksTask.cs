using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{

    public class GitListLocksTask : GitProcessListTask<GitLock>
    {
        private const string TaskName = "git lfs locks";
        private readonly string args;

        public GitListLocksTask(IPlatform platform,
                bool local,
                CancellationToken token = default)
            : base(platform, null, outputProcessor: new LocksOutputProcessor(), token: token)
        {
            Name = TaskName;
            args = "lfs locks --json";
            if (local)
            {
                args += " --local";
            }
        }

        public override string ProcessArguments => args;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Reading locks...";
    }
}

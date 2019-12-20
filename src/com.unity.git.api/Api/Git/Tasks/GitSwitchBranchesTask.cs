using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitSwitchBranchesTask : GitProcessTask<string>
    {
        private const string TaskName = "git checkout";
        private readonly string arguments;

        public GitSwitchBranchesTask(IPlatform platform,
            string branch,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            Name = TaskName;
            arguments = $"checkout {branch}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Switching branch...";
    }
}

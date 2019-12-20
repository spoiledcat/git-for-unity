using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitBranchCreateTask : GitProcessTask<string>
    {
        private const string TaskName = "git branch";
        private readonly string arguments;


        public GitBranchCreateTask(IPlatform platform,
            string newBranch, string baseBranch,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(newBranch, "newBranch");
            Guard.ArgumentNotNullOrWhiteSpace(baseBranch, "baseBranch");

            Name = TaskName;
            arguments = $"branch {newBranch} {baseBranch}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Creating branch...";
    }
}

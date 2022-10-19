using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteBranchDeleteTask : GitProcessTask<string>
    {
        private const string TaskName = "git push --delete";
        private readonly string arguments;

        public GitRemoteBranchDeleteTask(IPlatform platform,
            string remote, string branch,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            Name = TaskName;
            arguments = $"push {remote} --delete {branch}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Deleting remote branch...";
    }
}

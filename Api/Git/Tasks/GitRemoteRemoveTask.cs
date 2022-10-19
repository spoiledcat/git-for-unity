using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteRemoveTask : GitProcessTask<string>
    {
        private const string TaskName = "git remote rm";
        private readonly string arguments;

        public GitRemoteRemoveTask(IPlatform platform,
            string remote,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Name = TaskName;
            arguments = $"remote rm {remote}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Deleting remote...";
    }
}

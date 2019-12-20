using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteChangeTask : GitProcessTask<string>
    {
        private const string TaskName = "git remote set-url";
        private readonly string arguments;

        public GitRemoteChangeTask(IPlatform platform,
            string remote, string url,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            Name = TaskName;
            arguments = $"remote set-url {remote} {url}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Switching remotes...";
    }
}

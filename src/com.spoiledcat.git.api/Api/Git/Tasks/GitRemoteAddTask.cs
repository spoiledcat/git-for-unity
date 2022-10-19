using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteAddTask : GitProcessTask<string>
    {
        private const string TaskName = "git remote add";
        private readonly string arguments;

        public GitRemoteAddTask(IPlatform platform,
            string remote, string url,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            Name = TaskName;
            arguments = $"remote add {remote} {url}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Adding remote...";
    }
}

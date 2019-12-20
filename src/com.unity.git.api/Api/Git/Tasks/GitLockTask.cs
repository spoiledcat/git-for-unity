using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLockTask : GitProcessTask<string>
    {
        private const string TaskName = "git lfs lock";
        private readonly string args;

        public GitLockTask(IPlatform platform,
                string path,
                CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            args = $"lfs lock \"{path}\"";
        }

        public override string ProcessArguments => args;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Locking file...";
    }
}

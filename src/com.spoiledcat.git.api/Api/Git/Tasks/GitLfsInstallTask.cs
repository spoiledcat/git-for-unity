using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLfsInstallTask : GitProcessTask<string>
    {
        private const string TaskName = "git lsf install";

        public GitLfsInstallTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => "lfs install";
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Initializing LFS...";
    }
}

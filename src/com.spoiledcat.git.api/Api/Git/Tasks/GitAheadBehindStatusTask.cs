using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitAheadBehindStatusTask : GitProcessTask<GitAheadBehindStatus>
    {
        private const string TaskName = "git rev-list";
        private readonly string arguments;

        public GitAheadBehindStatusTask(IPlatform platform,
            string gitRef, string otherRef,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new GitAheadBehindStatusOutputProcessor(), token: token)
        {
            Name = TaskName;
            arguments = $"rev-list --left-right --count {gitRef}...{otherRef}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Querying status...";
    }
}

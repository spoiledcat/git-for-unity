using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitBranchDeleteTask : GitProcessTask<string>
    {
        private const string TaskName = "git branch -d";
        private readonly string arguments;

        public GitBranchDeleteTask(IPlatform platform,
            string branch, bool deleteUnmerged,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            Name = TaskName;
            arguments = !deleteUnmerged ? $"branch -d {branch}" : $"branch -D {branch}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Deleting branch...";
    }
}

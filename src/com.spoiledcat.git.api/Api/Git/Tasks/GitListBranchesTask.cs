using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitListLocalBranchesTask : GitProcessListTask<GitBranch>
    {
        private const string TaskName = "git list local branches";
        private const string Arguments = "branch -vv";

        public GitListLocalBranchesTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new BranchListOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => Arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Listing local branches...";
    }

    class GitListRemoteBranchesTask : GitProcessListTask<GitBranch>
    {
        private const string TaskName = "git list remote branches";
        private const string Arguments = "branch -vvr";

        public GitListRemoteBranchesTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new BranchListOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => Arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Listing remote branches...";
    }
}

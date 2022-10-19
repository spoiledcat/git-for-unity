using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteListTask : GitProcessListTask<GitRemote>
    {
        private const string TaskName = "git remote";

        public GitRemoteListTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new RemoteListOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => "remote -v";
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Listing remotes...";
    }
}

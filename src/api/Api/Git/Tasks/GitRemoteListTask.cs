using System.Threading;

namespace Unity.Git.Git.Tasks
{
    public class GitRemoteListTask : ProcessTaskWithListOutput<GitRemote>
    {
        private const string TaskName = "git remote";

        public GitRemoteListTask(CancellationToken token, BaseOutputListProcessor<GitRemote> processor = null)
            : base(token, processor ?? new RemoteListOutputProcessor())
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "remote -v"; } }
        public override string Message { get; set; } = "Listing remotes...";
    }
}

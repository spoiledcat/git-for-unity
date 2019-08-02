using System.Threading;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLockTask : ProcessTask<string>
    {
        private const string TaskName = "git lfs lock";

        public GitLockTask(string path,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Name = TaskName;
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            ProcessArguments = $"lfs lock \"{path}\"";
        }

        public override string ProcessArguments { get; }
        public override TaskAffinity Affinity => TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Locking file...";
    }
}

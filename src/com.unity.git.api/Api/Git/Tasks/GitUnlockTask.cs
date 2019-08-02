using System.Text;
using System.Threading;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitUnlockTask : ProcessTask<string>
    {
        private const string TaskName = "git lfs unlock";

        public GitUnlockTask(NPath path, bool force,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");

            Name = TaskName;
            var stringBuilder = new StringBuilder("lfs unlock ");

            if (force)
            {
                stringBuilder.Append("--force ");
            }

            stringBuilder.Append("\"");
            stringBuilder.Append(path.ToString(SlashMode.Forward));
            stringBuilder.Append("\"");

            ProcessArguments = stringBuilder.ToString();
        }

        public override string ProcessArguments { get; }

        public override TaskAffinity Affinity => TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Unlocking file...";

    }
}

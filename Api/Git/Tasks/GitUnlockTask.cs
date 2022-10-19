using System.Text;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitUnlockTask : GitProcessTask<string>
    {
        private const string TaskName = "git lfs unlock";
        private readonly string arguments;

        public GitUnlockTask(IPlatform platform,
            SPath path, bool force,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
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

            arguments = stringBuilder.ToString();
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Unlocking file...";

    }
}

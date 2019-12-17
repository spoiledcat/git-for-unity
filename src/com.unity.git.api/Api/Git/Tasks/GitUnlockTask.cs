using System.Text;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitUnlockTask : NativeProcessTask<string>
    {
        private const string TaskName = "git lfs unlock";

        public GitUnlockTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            SPath path, bool force,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
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

        public override string ProcessArguments { get; protected set; }

        public override TaskAffinity Affinity => TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Unlocking file...";

    }
}

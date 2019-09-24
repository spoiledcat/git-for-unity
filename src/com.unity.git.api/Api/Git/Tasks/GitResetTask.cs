using System;
using System.Threading;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitResetTask : ProcessTask<string>
    {
        private const string TaskName = "git reset";
        private readonly string arguments;

        public GitResetTask(string changeset, GitResetMode resetMode,
            CancellationToken token, IOutputProcessor<string> processor = null)
            : base(token, processor ?? new SimpleOutputProcessor())
        {
            Name = TaskName;
            arguments = $"reset {GetModeString(resetMode)} {changeset}";
        }

        private string GetModeString(GitResetMode resetMode)
        {
            switch (resetMode)
            {
                case GitResetMode.NonSpecified:
                    return string.Empty;
                case GitResetMode.Soft:
                    return "--soft";
                case GitResetMode.Keep:
                    return "--keep";
                case GitResetMode.Mixed:
                    return "--mixed";
                case GitResetMode.Hard:
                    return "--hard";
                default:
                    throw new ArgumentOutOfRangeException(nameof(resetMode), resetMode, null);
            }
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Writing configuration...";
    }
}

using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitResetTask : GitProcessTask<string>
    {
        private const string TaskName = "git reset";
        private readonly string arguments;

        public GitResetTask(IPlatform platform,
            string changeset, GitResetMode resetMode,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
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

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Writing configuration...";
    }
}

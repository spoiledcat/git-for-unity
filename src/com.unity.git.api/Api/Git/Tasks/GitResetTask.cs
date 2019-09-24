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
            var mode = "";
            mode +=
                resetMode == GitResetMode.NonSpecified ? "" :
                    resetMode == GitResetMode.Soft ? "--soft" :
                        resetMode == GitResetMode.Keep ? "--keep" :
                            resetMode == GitResetMode.Mixed ? "--mixed" :
                                "--hard";

            Name = TaskName;
            arguments = $"reset {mode} {changeset}";
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Writing configuration...";
    }
}

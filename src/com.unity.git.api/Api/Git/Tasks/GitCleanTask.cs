using System.Collections.Generic;
using System.Threading;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitCleanTask : ProcessTask<string>
    {
        private const string TaskName = "git clean";
        private readonly string arguments;

        public GitCleanTask(IEnumerable<string> files, CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "clean ";
            arguments += " -f ";
            arguments += " -q ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToNPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitCleanTask(CancellationToken token,
            IOutputProcessor<string> processor = null) : base(token, processor ?? new SimpleOutputProcessor())
        {
            arguments = "clean ";
            arguments += "-f";
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Removing untracked files...";
    }
}

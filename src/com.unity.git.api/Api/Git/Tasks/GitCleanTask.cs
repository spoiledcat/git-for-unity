using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitCleanTask : GitProcessTask<string>
    {
        private const string TaskName = "git clean";
        private readonly string arguments;

        public GitCleanTask(IPlatform platform,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "clean ";
            arguments += " -f ";
            arguments += " -q ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitCleanTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            arguments = "clean ";
            arguments += "-f";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Removing untracked files...";
    }
}

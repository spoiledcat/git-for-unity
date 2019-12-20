using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitAddTask : GitProcessTask<string>
    {
        private const string TaskName = "git add";
        private readonly string arguments;

        public GitAddTask(IPlatform platform,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "add ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitAddTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            arguments = "add -A";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Staging files...";
    }
}

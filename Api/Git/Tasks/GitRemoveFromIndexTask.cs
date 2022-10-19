using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitRemoveFromIndexTask : GitProcessTask<string>
    {
        private const string TaskName = "git reset HEAD";
        private readonly string arguments;

        public GitRemoveFromIndexTask(IPlatform platform,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");

            Name = TaskName;
            arguments = "reset HEAD";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Unstaging files...";
    }
}

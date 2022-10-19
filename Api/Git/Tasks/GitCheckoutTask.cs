using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitCheckoutTask : GitProcessTask<string>
    {
        private const string TaskName = "git checkout";
        private readonly string arguments;

        public GitCheckoutTask(IPlatform platform,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "checkout ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitCheckoutTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            arguments = "checkout -- .";
        }

        public GitCheckoutTask(IPlatform platform,
            string changeset,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "checkout ";
            arguments += changeset;
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }

            Message = "Checking out files at rev " + changeset.Substring(0, 7);
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Checking out files...";
    }
}

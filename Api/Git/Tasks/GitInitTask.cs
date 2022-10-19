using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitInitTask : GitProcessTask<string>
    {
        private const string TaskName = "git init";

        public GitInitTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, "init", outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string Message { get; set; } = "Initializing...";
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
    }
}

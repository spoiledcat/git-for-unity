using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitCountObjectsTask : GitProcessTask<int>
    {
        private const string TaskName = "git count-objects";

        public GitCountObjectsTask(IPlatform platform,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new GitCountObjectsProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => "count-objects";
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Counting git objects...";
    }
}

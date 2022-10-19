using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRevertTask : GitProcessTask<string>
    {
        private const string TaskName = "git revert";
        private readonly string arguments;

        public GitRevertTask(IPlatform platform,
            string changeset,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(changeset, "changeset");
            Name = TaskName;
            arguments = $"revert --no-edit {changeset}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Reverting commit...";
    }
}

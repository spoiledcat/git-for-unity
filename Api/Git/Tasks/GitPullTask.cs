using System.Text;
using System.Threading;
using Unity.Editor.Tasks;
using static System.String;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitPullTask : GitProcessTask<string>
    {
        private const string TaskName = "git pull";
        private readonly string arguments;

        public GitPullTask(IPlatform platform,
            string remote, string branch,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("pull");

            if (!IsNullOrEmpty(remote))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(remote);
            }

            if (!IsNullOrEmpty(branch))
            {
                stringBuilder.Append(" ");
                stringBuilder.Append(branch);
            }

            arguments = stringBuilder.ToString();
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Pulling...";
    }
}

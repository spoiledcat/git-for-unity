using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;
using static System.String;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitFetchTask : GitProcessTask<string>
    {
        private const string TaskName = "git fetch";
        private readonly string arguments;

        public GitFetchTask(IPlatform platform,
            string remote, bool prune = true, bool tags = true,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
            var args = new List<string> { "fetch" };
            
            if (prune)
            {
                args.Add("--prune");
            }

            if (tags)
            {
                args.Add("--tags");
            }

            if (!IsNullOrEmpty(remote))
            {
                args.Add(remote);
            }

            arguments = args.Join(" ");
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Fetching...";
    }
}

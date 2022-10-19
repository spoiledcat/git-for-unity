using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitStatusTask : GitProcessTask<GitStatus>
    {
        private const string TaskName = "git status";

        public GitStatusTask(IPlatform platform,
            IGitObjectFactory gitObjectFactory,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new GitStatusOutputProcessor(gitObjectFactory), token: token)
               
        {
            Name = TaskName;
        }

        public override string ProcessArguments => "-c i18n.logoutputencoding=utf8 -c core.quotepath=false --no-optional-locks status -b -u --porcelain";
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Listing changed files...";
    }
}

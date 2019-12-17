using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitStatusTask : NativeProcessTask<GitStatus>
    {
        private const string TaskName = "git status";

        public GitStatusTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            IGitObjectFactory gitObjectFactory,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new GitStatusOutputProcessor(gitObjectFactory), token: token)
               
        {
            Name = TaskName;
        }

        public override string ProcessArguments
        {
            get { return "-c i18n.logoutputencoding=utf8 -c core.quotepath=false --no-optional-locks status -b -u --porcelain"; }
        }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Listing changed files...";
    }
}

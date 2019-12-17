using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLogTask : NativeProcessListTask<GitLogEntry>
    {
        private const string TaskName = "git log";
        private const string baseArguments = @"-c i18n.logoutputencoding=utf8 -c core.quotepath=false log --pretty=format:""%H%n%P%n%aN%n%aE%n%aI%n%cN%n%cE%n%cI%n%B---GHUBODYEND---"" --name-status";
        private readonly string arguments;

        public GitLogTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            IGitObjectFactory gitObjectFactory,
            int numberOfCommits,
            CancellationToken token = default)
            : this(taskManager, processEnvironment, environment, gitObjectFactory, null, numberOfCommits, token)
        {}

        public GitLogTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            IGitObjectFactory gitObjectFactory,
            string file,
            CancellationToken token = default)
            : this(taskManager, processEnvironment, environment, gitObjectFactory, file, 0, token)
        {}

        public GitLogTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            IGitObjectFactory gitObjectFactory,
            string file = null, int numberOfCommits = 0,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new LogEntryOutputProcessor(gitObjectFactory), token: token)
        {
            Name = TaskName;
            arguments = baseArguments;
            if (numberOfCommits > 0)
                arguments += " -n " + numberOfCommits;

            if (file != null)
            {
                arguments += " -- ";
                arguments += " \"" + file + "\"";
            }
        }
        public override string ProcessArguments => arguments;
        public override string Message { get; set; } = "Loading the history...";
    }
}

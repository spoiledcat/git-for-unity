using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{

    public class GitListLocksTask : NativeProcessListTask<GitLock>
    {
        private const string TaskName = "git lfs locks";
        private readonly string args;

        public GitListLocksTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
                bool local,
                CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new LocksOutputProcessor(), token: token)
        {
            Name = TaskName;
            args = "lfs locks --json";
            if (local)
            {
                args += " --local";
            }
        }

        public override string ProcessArguments => args;
        public override string Message { get; set; } = "Reading locks...";
    }
}

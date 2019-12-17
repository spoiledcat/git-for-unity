using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitSwitchBranchesTask : NativeProcessTask<string>
    {
        private const string TaskName = "git checkout";
        private readonly string arguments;

        public GitSwitchBranchesTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string branch,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            Name = TaskName;
            arguments = String.Format("checkout {0}", branch);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Switching branch...";
    }
}

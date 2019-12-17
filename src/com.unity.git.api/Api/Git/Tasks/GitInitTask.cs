using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitInitTask : NativeProcessTask<string>
    {
        private const string TaskName = "git init";

        public GitInitTask(ITaskManager taskManager, IProcessEnvironment gitEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, gitEnvironment, environment.GitExecutablePath, "init", outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
            Affinity = TaskAffinity.Exclusive;
        }

        public override string Message { get; set; } = "Initializing...";
    }
}

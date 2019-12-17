using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitVersionTask : NativeProcessTask<TheVersion>
    {
        private const string TaskName = "git --version";

        public GitVersionTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new VersionOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "--version"; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
        public override string Message { get; set; } = "Reading git version...";
    }
}

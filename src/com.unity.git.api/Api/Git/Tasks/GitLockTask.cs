using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitLockTask : NativeProcessTask<string>
    {
        private const string TaskName = "git lfs lock";

        public GitLockTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
                string path,
                CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Name = TaskName;
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            ProcessArguments = $"lfs lock \"{path}\"";
        }

        public override string ProcessArguments { get; protected set; }
        public override TaskAffinity Affinity => TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Locking file...";
    }
}

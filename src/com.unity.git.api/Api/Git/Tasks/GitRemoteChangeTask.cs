using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteChangeTask : NativeProcessTask<string>
    {
        private const string TaskName = "git remote set-url";
        private readonly string arguments;

        public GitRemoteChangeTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string remote, string url,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(url, "url");

            Name = TaskName;
            arguments = String.Format("remote set-url {0} {1}", remote, url);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Switching remotes...";
    }
}

using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitRemoteListTask : NativeProcessListTask<GitRemote>
    {
        private const string TaskName = "git remote";

        public GitRemoteListTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new RemoteListOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments { get { return "remote -v"; } }
        public override string Message { get; set; } = "Listing remotes...";
    }
}

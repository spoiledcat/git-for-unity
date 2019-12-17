using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitListLocalBranchesTask : NativeProcessListTask<GitBranch>
    {
        private const string TaskName = "git list local branches";
        private const string Arguments = "branch -vv";

        public GitListLocalBranchesTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new BranchListOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => Arguments;
        public override string Message { get; set; } = "Listing local branches...";
    }


    class GitListRemoteBranchesTask : ProcessTaskWithListOutput<GitBranch>
    {
        private const string TaskName = "git list remote branches";
        private const string Arguments = "branch -vvr";

        public GitListRemoteBranchesTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new BranchListOutputProcessor(), token: token)
        {
            Name = TaskName;
        }

        public override string ProcessArguments => Arguments;
        public override string Message { get; set; } = "Listing remote branches...";
    }
}

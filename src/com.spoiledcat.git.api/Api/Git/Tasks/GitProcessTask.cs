using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitProcessTask : NativeProcessTask
    {
        public GitProcessTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            string gitExecutablePath, string arguments,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, gitExecutablePath, arguments, token)
        { }

        public GitProcessTask(IPlatform platform, string arguments,
            CancellationToken token = default)
            : base(platform.TaskManager, platform.ProcessManager.GitProcessEnvironment,
                platform.Environment.GitExecutablePath, arguments, token)
        { }
    }

    public class GitProcessTask<T> : NativeProcessTask<T>
    {
        public GitProcessTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            string gitExecutablePath, string arguments,
            IOutputProcessor<T> outputProcessor, CancellationToken token = default)
            : base(taskManager, processEnvironment,
                gitExecutablePath, arguments, outputProcessor, token)
        {}

        public GitProcessTask(IPlatform platform, string arguments,
            IOutputProcessor<T> outputProcessor, CancellationToken token = default)
            : base(platform.TaskManager, platform.ProcessManager.GitProcessEnvironment,
                platform.Environment.GitExecutablePath, arguments, outputProcessor, token)
        {}
    }

    public class GitProcessListTask<T> : NativeProcessListTask<T>
    {
        public GitProcessListTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            string gitExecutablePath, string arguments,
            IOutputProcessor<T, List<T>> outputProcessor, CancellationToken token = default)
            : base(taskManager, processEnvironment,
                gitExecutablePath, arguments, outputProcessor, token)
        {}

        public GitProcessListTask(IPlatform platform, string arguments,
            IOutputProcessor<T, List<T>> outputProcessor, CancellationToken token = default)
            : base(platform.TaskManager, platform.ProcessManager.GitProcessEnvironment,
                platform.Environment.GitExecutablePath, arguments, outputProcessor, token)
        {}
    }
}

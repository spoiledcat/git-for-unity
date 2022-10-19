using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    using IO;

    public class FindExecTask : NativeProcessTask<SPath>
    {
        public FindExecTask(ITaskManager taskManager, IProcessEnvironment processEnvironment, IEnvironment environment,
            string executable, CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.IsWindows ? "where" : "which", executable,
                new FirstLineIsPathOutputProcessor(), token)
        {}

        public FindExecTask(ITaskManager taskManager, IEnvironment environment,
            string executable, CancellationToken token = default)
            : base(taskManager, environment, environment.IsWindows ? "where" : "which", executable,
                new FirstLineIsPathOutputProcessor(), token)
        {}

        public override TaskAffinity Affinity { get; set; } = TaskAffinity.None;
    }
}

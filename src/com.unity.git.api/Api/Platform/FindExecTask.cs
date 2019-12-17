using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    using IO;

    public class FindExecTask : NativeProcessTask<SPath>
    {
        private readonly string arguments;

        public FindExecTask(ITaskManager taskManager, IEnvironment environment,
            string executable, CancellationToken token = default)
            : base(taskManager, environment, null, null, new FirstLineIsPathOutputProcessor(), token)
        {
            Name = environment.IsWindows ? "where" : "which";
            arguments = executable;
        }

        public override string ProcessName { get { return Name; } }
        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }
}

using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitCleanTask : NativeProcessTask<string>
    {
        private const string TaskName = "git clean";
        private readonly string arguments;

        public GitCleanTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "clean ";
            arguments += " -f ";
            arguments += " -q ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitCleanTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            arguments = "clean ";
            arguments += "-f";
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Removing untracked files...";
    }
}

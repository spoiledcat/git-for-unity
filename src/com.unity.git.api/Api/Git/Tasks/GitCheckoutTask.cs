using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    using IO;

    public class GitCheckoutTask : NativeProcessTask<string>
    {
        private const string TaskName = "git checkout";
        private readonly string arguments;

        public GitCheckoutTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "checkout ";
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }
        }

        public GitCheckoutTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            arguments = "checkout -- .";
        }

        public GitCheckoutTask(
            ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string changeset,
            IEnumerable<string> files,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNull(files, "files");
            Name = TaskName;

            arguments = "checkout ";
            arguments += changeset;
            arguments += " -- ";

            foreach (var file in files)
            {
                arguments += " \"" + file.ToSPath().ToString(SlashMode.Forward) + "\"";
            }

            Message = "Checking out files at rev " + changeset.Substring(0, 7);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Checking out files...";
    }
}

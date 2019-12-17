using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigUnSetTask : NativeProcessTask<string>
    {
        private readonly string arguments;

        public GitConfigUnSetTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string key, GitConfigSource configSource,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--unset" :
                configSource == GitConfigSource.Local ? "--local --unset" :
                configSource == GitConfigSource.User ? "--global --unset" :
                "--system --unset";
            arguments = String.Format("config {0} {1}", source, key);
            Name = String.Format("config {0} {1}", source, key);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Writing configuration...";
    }
}

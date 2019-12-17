using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigSetTask : NativeProcessTask<string>
    {
        private readonly string arguments;

        public GitConfigSetTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string key, string value, GitConfigSource configSource,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "" :
                    configSource == GitConfigSource.Local ? "--replace-all --local" :
                        configSource == GitConfigSource.User ? "--replace-all --global" :
                            "--replace-all --system";
            arguments = String.Format("config {0} {1} \"{2}\"", source, key, value);
            Name = String.Format("config {0} {1} \"{2}\"", source, key, new String('*', value.Length));
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Exclusive; } }
        public override string Message { get; set; } = "Writing configuration...";
    }
}

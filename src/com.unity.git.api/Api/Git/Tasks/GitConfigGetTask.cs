using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigGetAllTask : NativeProcessListTask<string>
    {
        private const string TaskName = "git config get";
        private readonly string arguments;

        public GitConfigGetAllTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string key, GitConfigSource configSource,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new StringListOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Name = TaskName;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
    }

    class GitConfigGetTask : NativeProcessTask<string>
    {
        private const string TaskName = "git config get";
        private readonly string arguments;

        public GitConfigGetTask(ITaskManager taskManager, IProcessEnvironment processEnvironment,
            IGitEnvironment environment,
            string key, GitConfigSource configSource,
            CancellationToken token = default)
            : base(taskManager, processEnvironment, environment.GitExecutablePath, null, outputProcessor: new FirstNonNullOutputProcessor<string>(), token: token)
        {
            Name = TaskName;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = String.Format("config {0} {1}", source, key);
        }

        public override string ProcessArguments { get { return arguments; } }
        public override TaskAffinity Affinity { get { return TaskAffinity.Concurrent; } }
        public override string Message { get; set; } = "Reading configuration...";
    }
}

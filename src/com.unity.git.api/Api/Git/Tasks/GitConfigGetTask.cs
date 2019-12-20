using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigGetAllTask : GitProcessListTask<string>
    {
        private const string TaskName = "git config get";
        private readonly string arguments;

        public GitConfigGetAllTask(IPlatform platform,
            string key, GitConfigSource configSource,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringListOutputProcessor(), token: token)
        {
            Guard.ArgumentNotNullOrWhiteSpace(key, nameof(key));
            Name = TaskName;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = $"config {source} {key}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity => TaskAffinity.Concurrent;
    }

    class GitConfigGetTask : GitProcessTask<string>
    {
        private const string TaskName = "git config get";
        private readonly string arguments;

        public GitConfigGetTask(IPlatform platform,
            string key, GitConfigSource configSource,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new FirstNonNullOutputProcessor<string>(), token: token)
        {
            Name = TaskName;
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--get-all" :
                configSource == GitConfigSource.Local ? "--get --local" :
                configSource == GitConfigSource.User ? "--get --global" :
                "--get --system";
            arguments = $"config {source} {key}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Reading configuration...";
    }
}

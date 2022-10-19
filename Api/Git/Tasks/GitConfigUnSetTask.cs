using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigUnSetTask : GitProcessTask<string>
    {
        private readonly string arguments;

        public GitConfigUnSetTask(IPlatform platform,
            string key, GitConfigSource configSource,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "--unset" :
                configSource == GitConfigSource.Local ? "--local --unset" :
                configSource == GitConfigSource.User ? "--global --unset" :
                "--system --unset";
            arguments = $"config {source} {key}";
            Name = $"config {source} {key}";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Writing configuration...";
    }
}

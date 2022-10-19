using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigListTask : GitProcessListTask<KeyValuePair<string, string>>
    {
        private const string TaskName = "git config list";
        private readonly string arguments;

        public GitConfigListTask(IPlatform platform,
            GitConfigSource configSource,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new ConfigOutputProcessor(), token: token)
        {
            Name = TaskName;
            var source = "";
            if (configSource != GitConfigSource.NonSpecified)
            {
                source = "--";
                source += configSource == GitConfigSource.Local
                    ? "local"
                    : (configSource == GitConfigSource.User
                        ? "system"
                        : "global");
            }
            arguments = $"config {source} -l";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Concurrent;
        public override string Message { get; set; } = "Reading configuration...";
    }
}

using System;
using System.Threading;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git.Tasks
{
    public class GitConfigSetTask : GitProcessTask<string>
    {
        private readonly string arguments;

        public GitConfigSetTask(IPlatform platform,
            string key, string value, GitConfigSource configSource,
            CancellationToken token = default)
            : base(platform, null, outputProcessor: new StringOutputProcessor(), token: token)
        {
            var source = "";
            source +=
                configSource == GitConfigSource.NonSpecified ? "" :
                    configSource == GitConfigSource.Local ? "--replace-all --local" :
                        configSource == GitConfigSource.User ? "--replace-all --global" :
                            "--replace-all --system";
            arguments = $"config {source} {key} \"{value}\"";
            Name = $"config {source} {key} \"{new string('*', value.Length)}\"";
        }

        public override string ProcessArguments => arguments;
        public override TaskAffinity Affinity { get; set; } = TaskAffinity.Exclusive;
        public override string Message { get; set; } = "Writing configuration...";
    }
}

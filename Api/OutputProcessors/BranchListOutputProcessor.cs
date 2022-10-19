using System;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class BranchListOutputProcessor : BaseOutputListProcessor<GitBranch>
    {
        private static readonly Regex trackingBranchRegex = new Regex(@"\[[\w]+\/.*\]");

        protected override bool ProcessLine(string line, out GitBranch result)
        {
            base.ProcessLine(line, out result);

            if (line == null)
                return false;

            var proc = new LineParser(line);
            if (proc.IsAtEnd)
                return false;

            try
            {
                string name;
                string trackingName = null;

                if (proc.Matches('*'))
                    proc.MoveNext();
                proc.SkipWhitespace();
                if (proc.Matches("(HEAD "))
                {
                    name = "detached";
                    proc.MoveToAfter(')');
                }
                else
                {
                    name = proc.ReadUntilWhitespace();
                }

                proc.ReadUntilWhitespaceTrim();
                if (proc.Matches(trackingBranchRegex))
                {
                    trackingName = proc.ReadChunk('[', ']');
                    var indexOf = trackingName.IndexOf(':');
                    if (indexOf != -1)
                    {
                        trackingName = trackingName.Substring(0, indexOf);
                    }
                }

                result = new GitBranch(name, trackingName);
                return true;
            }
            catch(Exception ex)
            {
                Logger.Warning(ex, "Unexpected input when listing branches");
            }

            return false;
        }
    }
}

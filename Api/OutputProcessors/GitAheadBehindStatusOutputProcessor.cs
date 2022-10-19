using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class GitAheadBehindStatusOutputProcessor : BaseOutputProcessor<GitAheadBehindStatus>
    {
        protected override bool ProcessLine(string line, out GitAheadBehindStatus result)
        {
            base.ProcessLine(line, out result);

            if (line == null)
            {
                return false;
            }

            var proc = new LineParser(line);

            var ahead = int.Parse(proc.ReadUntilWhitespace());
            var behind = int.Parse(proc.ReadToEnd());

            result = new GitAheadBehindStatus(ahead, behind);
            return true;
        }
    }
}

using System;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public class LinuxDiskUsageOutputProcessor : BaseOutputProcessor<int>
    {
        public override void LineReceived(string line)
        {
            if (line == null)
                return;

            int kb;
            var proc = new LineParser(line);
            if (int.TryParse(proc.ReadUntilWhitespace(), out kb))
                RaiseOnEntry(kb);
        }
    }
}

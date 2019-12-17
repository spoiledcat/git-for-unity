using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    public class GitCountObjectsProcessor : BaseOutputProcessor<int>
    {
        protected override bool ProcessLine(string line, out int result)
        {
            base.ProcessLine(line, out result);

            //parses 2488 objects, 4237 kilobytes
            try
            {
                var proc = new LineParser(line);

                proc.MoveToAfter(',');
                var kilobytes = int.Parse(proc.ReadUntilWhitespaceTrim());

                result = kilobytes;
                return true;
            }
            catch {}
            return false;
        }
    }
}

using System;
using System.Text.RegularExpressions;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class LfsVersionOutputProcessor : BaseOutputProcessor<TheVersion>
    {
        protected override bool ProcessLine(string line, out TheVersion result)
        {
            base.ProcessLine(line, out result);

            if (string.IsNullOrEmpty(line))
                return false;

            var parts = line.Split('/', ' ');
            if (parts.Length > 1)
            {
                result = TheVersion.Parse(parts[1]);
                return true;
            }

            return false;
        }
    }
}

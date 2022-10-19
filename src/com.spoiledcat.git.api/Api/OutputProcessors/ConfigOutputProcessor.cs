using System;
using System.Collections.Generic;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class ConfigOutputProcessor : BaseOutputListProcessor<KeyValuePair<string, string>>
    {
        protected override bool ProcessLine(string line, out KeyValuePair<string, string> result)
        {
            base.ProcessLine(line, out result);

            if (string.IsNullOrEmpty(line))
                return false;

            var eqs = line.IndexOf("=");
            if (eqs <= 0)
            {
                return false;
            }

            result = new KeyValuePair<string, string>(line.Substring(0, eqs), line.Substring(eqs + 1));
            return true;
        }
    }
}

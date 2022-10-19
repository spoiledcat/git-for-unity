using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class VersionOutputProcessor : BaseOutputProcessor<TheVersion>
    {
        public static Regex GitVersionRegex = new Regex(@"git version (.*)");

        protected override bool ProcessLine(string line, out TheVersion result)
        {
            base.ProcessLine(line, out result);

            if (string.IsNullOrEmpty(line))
                return false;

            var match = GitVersionRegex.Match(line);
            if (match.Groups.Count > 1)
            {
                result = TheVersion.Parse(match.Groups[1].Value);
                return true;
            }
            return false;
        }
    }
}

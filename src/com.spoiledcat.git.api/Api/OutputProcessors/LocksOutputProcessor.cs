using System;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    class LocksOutputProcessor : BaseOutputListProcessor<GitLock>
    {
        protected override bool ProcessLine(string line, out GitLock result)
        {
            base.ProcessLine(line, out result);

            if (string.IsNullOrEmpty(line))
                return false;

            try
            {
                var locks = line.FromJson<GitLock[]>(lowerCase: true);
                foreach (var lck in locks)
                {
                    RaiseOnEntry(lck);
                }
            }
            catch(Exception ex)
            {
                Logger.Error(ex, $"Failed to parse lock line {line}");
            }
            return false;
        }
    }
}

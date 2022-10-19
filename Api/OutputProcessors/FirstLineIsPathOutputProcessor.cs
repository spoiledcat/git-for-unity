using System;
using Unity.Editor.Tasks;
using Unity.VersionControl.Git.IO;

namespace Unity.VersionControl.Git
{
    class FirstLineIsPathOutputProcessor : FirstResultOutputProcessor<SPath>
    {
        public FirstLineIsPathOutputProcessor()
            : base((string line, out SPath result) =>
            {
                result = SPath.Default;
                if (string.IsNullOrEmpty(line))
                    return false;
                result = line.ToSPath();
                return true;
            })
        { }
    }
}

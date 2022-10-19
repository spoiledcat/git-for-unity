using sfw.net;

namespace Unity.VersionControl.Git
{
    using IO;

    static class FileEventExtensions
    {
        internal static string Describe(this Event fileEvent)
        {
            var directory = fileEvent.Directory.ToSPath();

            var fileA = directory.Combine(fileEvent.FileA);

            if (fileEvent.FileB == null)
            {
                return $"{{FileEvent: {fileEvent.Type} \"{fileA}\"}}";
            }

            var fileB = directory.Combine(fileEvent.FileB);
            return $"{{FileEvent: {fileEvent.Type} \"{fileA}\"->\"{fileB}\"}}";
        }
    }
}

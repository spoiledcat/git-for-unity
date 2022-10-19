using System.Reflection;

namespace Unity.VersionControl.Git
{
    public static class ApplicationConfiguration
    {
        public const int DefaultWebTimeout = 100*1000;
        public const int DefaultGitTimeout = 5000;
        public static int WebTimeout { get; set; } = DefaultWebTimeout;
        public static int GitTimeout { get; set; } = DefaultGitTimeout;
    }
}

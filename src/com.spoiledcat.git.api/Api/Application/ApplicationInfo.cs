#pragma warning disable 436
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace Unity.VersionControl.Git
{

    public static partial class ApplicationInfo
    {
#if GFU_DEBUG
        public const string ApplicationName = "Git for Unity Debug";
        public const string ApplicationProvider = "Unity";
        public const string ApplicationSafeName = "GitForUnityDebug";
#else
        public const string ApplicationName = "GitForUnity";
        public const string ApplicationProvider = "Unity";
        public const string ApplicationSafeName = "GitForUnity";
#endif
        public const string ApplicationDescription = "Git for Unity";

        public static string Version { get; } =  ThisAssembly.GetInformationalVersion();
    }
}

internal static partial class ThisAssembly {
    public static string GetInformationalVersion()
    {
        try
        {
            var attr = System.Attribute.GetCustomAttribute(typeof(ThisAssembly).Assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute)) as System.Reflection.AssemblyInformationalVersionAttribute;
            if (attr != null)
                return attr.InformationalVersion;
            var basePath = Platform.Instance?.Environment.ExtensionInstallPath ?? SPath.Default;
            if (!basePath.IsInitialized)
                return "0";
            var version = basePath.Parent.Combine("version.json").ReadAllText().FromJson<VersionJson>(true);
            return TheVersion.Parse(version.version).Version;
        }
        catch
        {
            return "0";
        }
    }

    public class VersionJson
    {
        public string version;
    }
}

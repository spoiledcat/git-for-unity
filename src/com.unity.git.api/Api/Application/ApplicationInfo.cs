#pragma warning disable 436
using Unity.VersionControl.Git;

namespace Unity.VersionControl.Git
{
    public static partial class ApplicationInfo
    {
#if GFU_DEBUG_BUILD
        public const string ApplicationName = "GitHub for Unity Debug";
        public const string ApplicationProvider = "GitHub";
        public const string ApplicationSafeName = "GitHubUnity-dev";
#else
        public const string ApplicationName = "GitHubUnity";
        public const string ApplicationProvider = "GitHub";
        public const string ApplicationSafeName = "GitHubUnity";
#endif
        public const string ApplicationDescription = "GitHub for Unity";

#if GFU_DEBUG_BUILD
/*
        For external contributors, we have bundled a developer OAuth application
        called `GitHub for Unity (dev)` so that you can complete the sign in flow
        locally without needing to configure your own application.
        This is for testing only and it is (obviously) public, proceed with caution.

        For a release build, you should create a new oauth application on github.com,
        copy the `common/ApplicationInfo_Local.cs-example`
        template to `common/ApplicationInfo_Local.cs` and fill out the `myClientId` and
        `myClientSecret` fields for your oauth app.
 */
        public static string ClientId { get; private set; } = "924a97f36926f535e72c";
        public static string ClientSecret { get; private set; } = "b4fa550b7f8e38034c6b1339084fa125eebb6155";
#else
        public static string ClientId { get; private set; } = "";
        public static string ClientSecret { get; private set; } = "";
#endif

        public static string Version { get; } =  ThisAssembly.GetInformationalVersion();

        static partial void SetClientData();

        static ApplicationInfo()
        {
            SetClientData();
        }
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
            var basePath = Platform.Instance?.Environment.ExtensionInstallPath ?? NPath.Default;
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

using System;
using System.Globalization;
using System.Linq;
using TestUtils;
using Unity.VersionControl.Git;

namespace IntegrationTests
{


    class IntegrationTestEnvironment : IEnvironment
    {
        private static readonly ILogging logger = LogHelper.GetLogger<IntegrationTestEnvironment>();
        private readonly bool enableTrace;

        private readonly DefaultEnvironment defaultEnvironment;

        public IntegrationTestEnvironment(ICacheContainer cacheContainer,
            NPath repoPath,
            NPath solutionDirectory,
            CreateEnvironmentOptions options = null,
            bool enableTrace = false,
            bool initializeRepository = true)
        {
            defaultEnvironment = new DefaultEnvironment(cacheContainer);

            defaultEnvironment.FileSystem.SetCurrentDirectory(repoPath);
            options = options ?? new CreateEnvironmentOptions(NPath.SystemTemp.Combine(ApplicationInfo.ApplicationName, "IntegrationTests"));

            var environmentPath = options.UserProfilePath;

            LocalAppData = environmentPath.Combine("User");
            UserCachePath = LocalAppData.Combine("Cache");
            CommonAppData = environmentPath.Combine("System");
            SystemCachePath = CommonAppData.Combine("Cache");

            var installPath = solutionDirectory.Parent.Parent.Parent.Combine("src", "api", "Api");

            Initialize(UnityVersion, installPath, solutionDirectory, NPath.Default, repoPath.Combine("Assets"));

            InitializeRepository(initializeRepository ? (NPath?)repoPath : null);

            this.enableTrace = enableTrace;

            if (enableTrace)
            {
                logger.Trace("EnvironmentPath: \"{0}\" SolutionDirectory: \"{1}\" ExtensionInstallPath: \"{2}\"",
                    environmentPath, solutionDirectory, ExtensionInstallPath);
            }
        }

        public void Initialize(string unityVersion, NPath extensionInstallPath, NPath unityPath, NPath unityContentsPath, NPath assetsPath)
        {
            defaultEnvironment.Initialize(unityVersion, extensionInstallPath, unityPath, unityContentsPath, assetsPath);
            defaultEnvironment.LocalSettings.SettingsPath.DeleteIfExists();
            defaultEnvironment.UserSettings.SettingsPath.DeleteIfExists();
            defaultEnvironment.SystemSettings.SettingsPath.DeleteIfExists();
        }

        public void InitializeRepository(NPath? expectedPath = null)
        {
            defaultEnvironment.InitializeRepository(expectedPath);
        }

        public string ExpandEnvironmentVariables(string name)
        {
            return defaultEnvironment.ExpandEnvironmentVariables(name);
        }

        public string GetEnvironmentVariable(string v)
        {
            var environmentVariable = defaultEnvironment.GetEnvironmentVariable(v);
            if (enableTrace)
            {
                logger.Trace("GetEnvironmentVariable: {0}={1}", v, environmentVariable);
            }
            return environmentVariable;
        }


        public string GetEnvironmentVariableKey(string name)
        {
            return defaultEnvironment.GetEnvironmentVariableKey(name);
        }

        private static string GetEnvironmentVariableKeyInternal(string name)
        {
            return Environment.GetEnvironmentVariables().Keys.Cast<string>()
                              .FirstOrDefault(k => string.Compare(name, k, true, CultureInfo.InvariantCulture) == 0) ?? name;
        }

        public string GetSpecialFolder(Environment.SpecialFolder folder)
        {
            var ensureDirectoryExists = UserCachePath.Parent.EnsureDirectoryExists(folder.ToString());
            var specialFolderPath = ensureDirectoryExists.ToString();

            if (enableTrace)
            {
                logger.Trace("GetSpecialFolder: {0}", specialFolderPath);
            }

            return specialFolderPath;
        }

        public string UserProfilePath => UserCachePath.Parent.CreateDirectory("user profile path");

        public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

        public string NewLine => Environment.NewLine;
        public string UnityVersion => "5.6";

        public bool IsCustomGitExecutable => defaultEnvironment.IsCustomGitExecutable;
        public NPath GitExecutablePath => defaultEnvironment.GitExecutablePath;
        public NPath GitInstallPath => defaultEnvironment.GitInstallPath;
        public NPath GitLfsInstallPath => defaultEnvironment.GitLfsInstallPath;
        public NPath GitLfsExecutablePath => defaultEnvironment.GitLfsExecutablePath;
        public GitInstaller.GitInstallationState GitInstallationState { get { return defaultEnvironment.GitInstallationState; } set { defaultEnvironment.GitInstallationState = value; } }
        public GitInstaller.GitInstallDetails GitDefaultInstallation { get => defaultEnvironment.GitDefaultInstallation; set => defaultEnvironment.GitDefaultInstallation = value; }

        public NPath NodeJsExecutablePath { get; set; }

        public NPath OctorunScriptPath { get; set; }

        public bool IsWindows => defaultEnvironment.IsWindows;
        public bool IsLinux => defaultEnvironment.IsLinux;
        public bool IsMac => defaultEnvironment.IsMac;
        public bool Is32Bit => defaultEnvironment.Is32Bit;

        public NPath UnityApplication => defaultEnvironment.UnityApplication;

        public NPath UnityApplicationContents => defaultEnvironment.UnityApplicationContents;

        public NPath UnityAssetsPath => defaultEnvironment.UnityAssetsPath;

        public NPath UnityProjectPath => defaultEnvironment.UnityProjectPath;

        public NPath ExtensionInstallPath => defaultEnvironment.ExtensionInstallPath;

        public NPath UserCachePath { get => defaultEnvironment.UserCachePath; set => defaultEnvironment.UserCachePath = value; }
        public NPath SystemCachePath { get => defaultEnvironment.SystemCachePath; set => defaultEnvironment.SystemCachePath = value; }
        public NPath LocalAppData { get => defaultEnvironment.LocalAppData; set => defaultEnvironment.LocalAppData = value; }
        public NPath CommonAppData { get => defaultEnvironment.CommonAppData; set => defaultEnvironment.CommonAppData = value; }

        public NPath LogPath => defaultEnvironment.LogPath;

        public NPath RepositoryPath => defaultEnvironment.RepositoryPath;

        public IRepository Repository { get { return defaultEnvironment.Repository; } set { defaultEnvironment.Repository = value; } }
        public IUser User { get { return defaultEnvironment.User; } set { defaultEnvironment.User = value; } }
        public IFileSystem FileSystem { get { return defaultEnvironment.FileSystem; } set { defaultEnvironment.FileSystem = value; } }
        public string ExecutableExtension => defaultEnvironment.ExecutableExtension;

        public ICacheContainer CacheContainer => defaultEnvironment.CacheContainer;
        public ISettings LocalSettings => defaultEnvironment.LocalSettings;
        public ISettings SystemSettings => defaultEnvironment.SystemSettings;
        public ISettings UserSettings => defaultEnvironment.UserSettings;
        public IOAuthCallbackManager OAuthCallbackListener { get; }
    }
}

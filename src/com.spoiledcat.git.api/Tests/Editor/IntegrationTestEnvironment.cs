using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;
using ILogging = Unity.Editor.Tasks.Logging.ILogging;
using LogHelper = Unity.Editor.Tasks.Logging.LogHelper;

namespace IntegrationTests
{
    class IntegrationTestEnvironment : IGitEnvironment
    {
        private readonly SPath sharedTestEnvironmentPath;
        private readonly SPath testPath;
        private const string logFile = "github-unity.log";

        public IntegrationTestEnvironment(SPath testPath, SPath sharedTestPath, string applicationName)
        {
            Guard.ArgumentNotNull(applicationName, nameof(applicationName));

            ApplicationName = applicationName;

            this.sharedTestEnvironmentPath = sharedTestPath.Combine("git-unity-test-environment").EnsureDirectoryExists();
            this.testPath = testPath.Combine("SystemData").EnsureDirectoryExists();

            LocalAppData = GetFolder(Folders.LocalApplicationData);
            CommonAppData = GetFolder(Folders.CommonApplicationData);

            UserCachePath = LocalAppData.Combine(applicationName).EnsureDirectoryExists();
            SystemCachePath = CommonAppData.Combine(applicationName).EnsureDirectoryExists();

            LogPath = GetFolder(Folders.Logs).Combine(applicationName).EnsureDirectoryExists().Combine(logFile);
            GitDefaultInstallation = new GitInstaller.GitInstallDetails(sharedTestEnvironmentPath, this);
        }

        public IntegrationTestEnvironment(ICacheContainer cacheContainer, SPath testPath, SPath sharedTestPath, string applicationName)
            : this(testPath, sharedTestPath, applicationName)
        {
            this.CacheContainer = cacheContainer;
        }

        public SPath GetFolder(Folders folder)
        {
            switch (folder)
            {
                case Folders.CommonApplicationData:
                    return testPath.Combine("UserProfile", "CommonAppData");
                case Folders.Logs:
                    return testPath.Combine("UserProfile", "Logs");
            }
            return testPath.Combine("UserProfile", "LocalAppData");
        }

        /// <summary>
        /// This is for tests to reset the static OS flags
        /// </summary>
        public static void Reset()
        {
            //onWindows = null;
            //onLinux = null;
            //onMac = null;
        }

        public IGitEnvironment Initialize(SPath extensionInstallPath, string projectPath, string unityVersion = null, string EditorApplication_applicationPath = null, string EditorApplication_applicationContentsPath = null)
        {
            Initialize(projectPath, unityVersion, EditorApplication_applicationPath, EditorApplication_applicationContentsPath);
            ExtensionInstallPath = extensionInstallPath;
            User = new User(CacheContainer);
            UserSettings = new UserSettings(this);
            LocalSettings = new LocalSettings(this);
            SystemSettings = new SystemSettings(this);
            return this;
        }

        public void InitializeRepository(SPath? repositoryPath = null)
        {
            SPath expectedRepositoryPath;
            if (!RepositoryPath.IsInitialized || (repositoryPath != null && RepositoryPath != repositoryPath.Value))
            {
                Guard.NotNull(this, UnityProjectPath, nameof(UnityProjectPath));

                expectedRepositoryPath = repositoryPath != null ? repositoryPath.Value : UnityProjectPath.ToSPath();

                if (!expectedRepositoryPath.Exists(".git"))
                {
                    SPath reporoot = UnityProjectPath.ToSPath().RecursiveParents.FirstOrDefault(d => d.Exists(".git"));
                    if (reporoot.IsInitialized)
                        expectedRepositoryPath = reporoot;
                }
            }
            else
            {
                expectedRepositoryPath = RepositoryPath;
            }

            if (expectedRepositoryPath.Exists(".git"))
            {
                SPath.FileSystem = new FileSystem(expectedRepositoryPath);
                RepositoryPath = expectedRepositoryPath;
                Repository = new Repository(RepositoryPath, CacheContainer);
            }
        }

        public SPath LogPath { get; }
        public SPath ExtensionInstallPath { get; set; }
        public SPath UserCachePath { get; set; }
        public SPath SystemCachePath { get; set; }
        public SPath LocalAppData { get; set; }
        public SPath CommonAppData { get; set; }

        public bool IsCustomGitExecutable => GitInstallationState?.IsCustomGitPath ?? false;
        public SPath GitInstallPath => GitInstallationState?.GitInstallationPath ?? SPath.Default;
        public SPath GitExecutablePath => GitInstallationState?.GitExecutablePath ?? SPath.Default;
        public SPath GitLfsInstallPath => GitInstallationState?.GitLfsInstallationPath ?? SPath.Default;
        public SPath GitLfsExecutablePath => GitInstallationState?.GitLfsExecutablePath ?? SPath.Default;
        public GitInstaller.GitInstallationState GitInstallationState
        {
            get
            {
                return SystemSettings.Get<GitInstaller.GitInstallationState>(Constants.GitInstallationState, new GitInstaller.GitInstallationState());
            }
            set
            {
                if (value == null)
                    SystemSettings.Unset(Constants.GitInstallationState);
                else
                    SystemSettings.Set<GitInstaller.GitInstallationState>(Constants.GitInstallationState, value);
            }
        }

        public GitInstaller.GitInstallDetails GitDefaultInstallation { get; set; }

        public SPath RepositoryPath { get; private set; }
        public ICacheContainer CacheContainer { get; private set; }
        public IRepository Repository { get; set; }
        public IUser User { get; set; }
        public ISettings LocalSettings { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ISettings UserSettings { get; protected set; }
        protected static ILogging Logger { get; } = LogHelper.GetLogger<ApplicationEnvironment>();

        #region IEnvironment implementation

        public virtual IEnvironment Initialize(
            string projectPath,
            string unityVersion = null,
            string EditorApplication_applicationPath = default,
            string EditorApplication_applicationContentsPath = default
        )
        {
            UnityProjectPath = projectPath;
            UnityVersion = unityVersion;
            UnityApplication = EditorApplication_applicationPath;
            UnityApplicationContents = EditorApplication_applicationContentsPath;

            return this;
        }

        public string ExpandEnvironmentVariables(string name)
        {
            var key = GetEnvironmentVariableKey(name);
            return Environment.ExpandEnvironmentVariables(key);
        }

        public string GetEnvironmentVariable(string name)
        {
            var key = GetEnvironmentVariableKey(name);
            return Environment.GetEnvironmentVariable(key);
        }

        public string GetEnvironmentVariableKey(string name)
        {
            return GetEnvironmentVariableKeyInternal(name);
        }

        private static string GetEnvironmentVariableKeyInternal(string name)
        {
            return Environment.GetEnvironmentVariables().Keys.Cast<string>()
                                        .FirstOrDefault(k => string.Compare(name, k, true, CultureInfo.InvariantCulture) == 0) ?? name;
        }

        public string ApplicationName { get; }
        public string UnityVersion { get; set; }
        public string UnityApplication { get; set; }
        public string UnityApplicationContents { get; set; }
        public string UnityProjectPath { get; set; }

        public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

        public string NewLine => IsWindows ? "\r\n" : "\n";

        public bool Is32Bit => IntPtr.Size == 4;

        public string ExecutableExtension => IsWindows ? ".exe" : string.Empty;

        private bool? isLinux;
        private bool? isMac;
        private bool? isWindows;
        public bool IsWindows
        {
            get
            {
                if (isWindows.HasValue)
                    return isWindows.Value;
                return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
            }
            set => isWindows = value;
        }

        public bool IsLinux
        {
            get
            {
                if (isLinux.HasValue)
                    return isLinux.Value;
                return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
            }
            set => isLinux = value;
        }

        public bool IsMac
        {
            get
            {
                if (isMac.HasValue)
                    return isMac.Value;
                // most likely it'll return the proper id but just to be on the safe side, have a fallback
                return Environment.OSVersion.Platform == PlatformID.MacOSX ||
                        (Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
            }
            set => isMac = value;
        }
        #endregion
    }
}

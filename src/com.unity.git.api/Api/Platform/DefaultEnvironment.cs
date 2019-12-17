using Unity.VersionControl.Git;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Editor.Tasks;

namespace Unity.VersionControl.Git
{
    using IO;

    public class DefaultEnvironment : UnityEnvironment, IGitEnvironment
    {
        private const string logFile = "github-unity.log";

        public DefaultEnvironment() : base(ApplicationInfo.ApplicationName)
        {
            if (IsWindows)
            {
                LocalAppData = GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData).ToSPath();
                CommonAppData = GetSpecialFolder(Environment.SpecialFolder.CommonApplicationData).ToSPath();
            }
            else if (IsMac)
            {
                LocalAppData = SPath.HomeDirectory.Combine("Library", "Application Support");
                // there is no such thing on the mac that is guaranteed to be user accessible (/usr/local might not be)
                CommonAppData = GetSpecialFolder(Environment.SpecialFolder.ApplicationData).ToSPath();
            }
            else
            {
                LocalAppData = GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData).ToSPath();
                CommonAppData = GetSpecialFolder(Environment.SpecialFolder.ApplicationData).ToSPath();
            }

            UserCachePath = LocalAppData.Combine(ApplicationInfo.ApplicationName);
            SystemCachePath = CommonAppData.Combine(ApplicationInfo.ApplicationName);
            if (IsMac)
            {
                LogPath = SPath.HomeDirectory.Combine("Library/Logs").Combine(ApplicationInfo.ApplicationName).Combine(logFile);
            }
            else
            {
                LogPath = UserCachePath.Combine(logFile);
            }
            LogPath.EnsureParentDirectoryExists();
            GitDefaultInstallation = new GitInstaller.GitInstallDetails(UserCachePath, this);
        }

        public DefaultEnvironment(ICacheContainer cacheContainer) : this()
        {
            this.CacheContainer = cacheContainer;
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

        public void Initialize(SPath extensionInstallPath)
        {
            ExtensionInstallPath = extensionInstallPath;
            User = new User(CacheContainer);
            UserSettings = new UserSettings(this);
            LocalSettings = new LocalSettings(this);
            SystemSettings = new SystemSettings(this);
        }

        public void InitializeRepository(SPath? repositoryPath = null)
        {
            Guard.NotNull(this, FileSystem, nameof(FileSystem));

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

            //FileSystem.SetCurrentDirectory(expectedRepositoryPath);
            if (expectedRepositoryPath.Exists(".git"))
            {
                RepositoryPath = expectedRepositoryPath;
                Repository = new Repository(RepositoryPath, CacheContainer);
            }
        }

        public string GetSpecialFolder(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }

        public SPath LogPath { get; }
        public IFileSystem FileSystem { get { return SPath.FileSystem; } set { SPath.FileSystem = value; } }
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
        protected static ILogging Logger { get; } = LogHelper.GetLogger<DefaultEnvironment>();
    }
}

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NCrunch.Framework;
using NUnit.Framework;
using Unity.VersionControl.Git;

namespace IntegrationTests
{
    [Isolated]
    class BaseIntegrationTest : BaseTest
    {
        protected NPath TestApp => TestLocation.Combine("CommandLine.exe");
        public IRepositoryManager RepositoryManager { get; set; }
        protected IApplicationManager ApplicationManager { get; set; }
        protected ITaskManager TaskManager { get; set; }
        protected IPlatform Platform { get; set; }
        protected IProcessManager ProcessManager { get; set; }
        protected IProcessEnvironment GitEnvironment => Platform.GitEnvironment;
        protected IGitClient GitClient { get; set; }
        public IEnvironment Environment { get; set; }

        protected NPath DotGitConfig { get; set; }
        protected NPath DotGitHead { get; set; }
        protected NPath DotGitIndex { get; set; }
        protected NPath RemotesPath { get; set; }
        protected NPath BranchesPath { get; set; }
        protected NPath DotGitPath { get; set; }
        protected NPath TestRepoMasterCleanSynchronized { get; set; }
        protected NPath TestRepoMasterCleanUnsynchronized { get; set; }
        protected NPath TestRepoMasterCleanUnsynchronizedRussianLanguage { get; set; }
        protected NPath TestRepoMasterDirtyUnsynchronized { get; set; }
        protected NPath TestRepoMasterTwoRemotes { get; set; }

        protected static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        public IRepository Repository => Environment.Repository;

        protected void InitializeEnvironment(NPath repoPath,
            bool enableEnvironmentTrace = false,
            bool initializeRepository = true
            )
        {
            var environment = CreateEnvironmentInPersistentLocation(repoPath, enableEnvironmentTrace);
            if (initializeRepository)
                environment.InitializeRepository(repoPath);

            environment.NodeJsExecutablePath = TestApp;
            environment.OctorunScriptPath = TestApp;
            Environment = environment;
        }

        protected void InitializePlatform(NPath repoPath,
            bool enableEnvironmentTrace = true,
            string testName = "")
        {
            InitializeTaskManager();

            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);
        }

        protected override ITaskManager InitializeTaskManager()
        {
            TaskManager = base.InitializeTaskManager();
            ApplicationManager = new ApplicationManagerBase(SyncContext, Environment);
            return TaskManager;
        }

        protected IEnvironment InitializePlatformAndEnvironment(NPath repoPath,
            bool enableEnvironmentTrace = false,
            Action<IRepositoryManager> onRepositoryManagerCreated = null,
            [CallerMemberName] string testName = "")
        {
            InitializeEnvironment(repoPath, enableEnvironmentTrace, true);
            InitializePlatform(repoPath, enableEnvironmentTrace: enableEnvironmentTrace, testName: testName);
            SetupGit(Environment.UserCachePath, testName);

            DotGitPath = repoPath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath = DotGitPath.ReadAllLines().Where(x => x.StartsWith("gitdir:"))
                                       .Select(x => x.Substring(7).Trim().ToNPath()).First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");

            RepositoryManager = Unity.VersionControl.Git.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, repoPath);
            RepositoryManager.Initialize();

            onRepositoryManagerCreated?.Invoke(RepositoryManager);

            Environment.Repository?.Initialize(RepositoryManager, TaskManager);

            RepositoryManager.Start();
            Environment.Repository?.Start();
            return Environment;
        }

        protected void SetupGit(NPath pathToSetupGitInto, string testName)
        {
            var installDetails = new GitInstaller.GitInstallDetails(pathToSetupGitInto, Environment);
            var state = installDetails.GetDefaults();
            Environment.GitInstallationState = state;
            GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);

            if (installDetails.GitExecutablePath.FileExists() && installDetails.GitLfsExecutablePath.FileExists())
                return;

            var key = installDetails.GitManifest.FileNameWithoutExtension + "_updatelastCheckTime";
            Environment.UserSettings.Set(key, DateTimeOffset.Now);

            var localCache = TestLocation.Combine("Resources");
            localCache.CopyFiles(pathToSetupGitInto, true);
            // skip checking for updates

            state.GitPackage = DugiteReleaseManifest.Load(installDetails.GitManifest, GitInstaller.GitInstallDetails.GitPackageFeed, Environment);
            var asset = state.GitPackage.DugitePackage;
            state.GitZipPath = installDetails.ZipPath.Combine(asset.Name);

            installDetails.GitInstallationPath.DeleteIfExists();

            state.GitZipPath.EnsureParentDirectoryExists();

            var gitExtractPath = TestBasePath.Combine("setup", "git_zip_extract_zip_paths").EnsureDirectoryExists();
            var source = new UnzipTask(TaskManager.Token, state.GitZipPath, gitExtractPath, null, Environment.FileSystem)
                            .RunSynchronously();

            installDetails.GitInstallationPath.EnsureParentDirectoryExists();
            source.Move(installDetails.GitInstallationPath);
        }

        public override void OnSetup()
        {
            base.OnSetup();

            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanUnsynchronizedRussianLanguage = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync_with_russian_language");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");
            TestRepoMasterTwoRemotes = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_two_remotes");

            InitializeTaskManager();
        }

        [TearDown]
        public override void OnTearDown()
        {
            TaskManager.Dispose();
            Environment?.CacheContainer.Dispose();
            BranchesCache.Instance = null;
            GitAheadBehindCache.Instance = null;
            GitLocksCache.Instance = null;
            GitLogCache.Instance = null;
            GitStatusCache.Instance = null;
            GitUserCache.Instance = null;
            RepositoryInfoCache.Instance = null;

            base.OnTearDown();
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using NCrunch.Framework;
using NUnit.Framework;
using TestUtils;
using Unity.VersionControl.Git;

namespace IntegrationTests
{
    [Isolated]
    class BaseIntegrationTest
    {
        protected NPath TestLocation => System.Reflection.Assembly.GetExecutingAssembly().Location.ToNPath().Parent;
        protected NPath TestApp => TestLocation.Combine("CommandLine.exe");
        public IRepositoryManager RepositoryManager { get; set; }
        protected IApplicationManager ApplicationManager { get; set; }
        protected ILogging Logger { get; set; }
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
        protected SynchronizationContext SyncContext { get; set; }


        protected NPath TestBasePath { get; set; }
        public IRepository Repository => Environment.Repository;

        protected TestUtils.TestSubstituteFactory Factory { get; set; }
        protected static NPath SolutionDirectory => TestContext.CurrentContext.TestDirectory.ToNPath();

        protected void InitializeEnvironment(NPath repoPath,
            bool enableEnvironmentTrace = false,
            bool initializeRepository = true
            )
        {
            var cacheContainer = new CacheContainer();
            cacheContainer.SetCacheInitializer(CacheType.Branches, () => BranchesCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitAheadBehind, () => GitAheadBehindCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitLocks, () => GitLocksCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitLog, () => GitLogCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitStatus, () => GitStatusCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitUser, () => GitUserCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.RepositoryInfo, () => RepositoryInfoCache.Instance);

            var environment = new IntegrationTestEnvironment(cacheContainer,
               repoPath,
               SolutionDirectory,
               enableTrace: enableEnvironmentTrace,
               initializeRepository: initializeRepository);
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

        protected void InitializeTaskManager()
        {
            TaskManager = new TaskManager();
            SyncContext = new ThreadSynchronizationContext(TaskManager.Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(SyncContext);
            ApplicationManager = new ApplicationManagerBase(SyncContext, Environment);
        }

        protected IEnvironment InitializePlatformAndEnvironment(NPath repoPath,
            bool enableEnvironmentTrace = false,
            Action<IRepositoryManager> onRepositoryManagerCreated = null,
            [CallerMemberName] string testName = "")
        {
            InitializeEnvironment(repoPath, enableEnvironmentTrace, true);
            InitializePlatform(repoPath, enableEnvironmentTrace: enableEnvironmentTrace, testName: testName);
            SetupGit(Environment.LocalAppData, testName);

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

            state.GitPackage = DugiteReleaseManifest.Load(installDetails.GitManifest, installDetails.GitPackageFeed, Environment);
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

        [OneTimeSetUp]
        public virtual void TestFixtureSetUp()
        {
            Logger = LogHelper.GetLogger(GetType());
            Factory = new TestUtils.TestSubstituteFactory();
            Guard.InUnitTestRunner = true;
        }

        [OneTimeTearDown]
        public virtual void TestFixtureTearDown()
        {
        }

        [SetUp]
        public virtual void OnSetup()
        {
            TestBasePath = NPath.CreateTempDirectory("integration tests");
            NPath.FileSystem.SetCurrentDirectory(TestBasePath);
            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanUnsynchronizedRussianLanguage = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync_with_russian_language");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");
            TestRepoMasterTwoRemotes = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_two_remotes");

            InitializeTaskManager();
        }

        [TearDown]
        public virtual void OnTearDown()
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

            Logger.Debug("Deleting TestBasePath: {0}", TestBasePath.ToString());
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    TestBasePath.Delete();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }
            if (TestBasePath.Exists())
                Logger.Warning("Error deleting TestBasePath: {0}", TestBasePath.ToString());

            NPath.FileSystem = null;
        }

        protected virtual void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
        {
            watch = new Stopwatch();
            logger = LogHelper.GetLogger(testName);
            logger.Trace("Starting test");
        }

        protected virtual void EndTest(ILogging logger)
        {
            logger.Trace("Ending test");
        }

        protected virtual void StartTrackTime(Stopwatch watch, ILogging logger = null, string message = "")
        {
            if (!String.IsNullOrEmpty(message))
                logger.Trace(message);
            watch.Reset();
            watch.Start();
        }

        protected virtual void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
        {
            watch.Stop();
            logger.Trace($"Time: {watch.ElapsedMilliseconds}");
        }
    }
}

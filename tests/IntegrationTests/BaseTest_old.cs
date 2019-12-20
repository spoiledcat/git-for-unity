using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using NCrunch.Framework;
using NUnit.Framework;
using TestUtils;
using Unity.Editor.Tasks;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace IntegrationTests
{
    [Isolated]
    class BaseTest
    {
        protected SPath TestLocation => System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent;
        protected SPath TestBasePath { get; set; }
        protected static SPath SolutionDirectory => TestContext.CurrentContext.TestDirectory.ToSPath();
        protected ILogging Logger { get; set; }
        protected TestSubstituteFactory Factory { get; set; }
        protected SynchronizationContext SyncContext { get; set; }

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
            TestBasePath = SPath.CreateTempDirectory("integration tests");
            //SPath.FileSystem.SetCurrentDirectory(TestBasePath);
        }

        [TearDown]
        public virtual void OnTearDown()
        {
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

            SPath.FileSystem = null;
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


        protected IntegrationTestEnvironment CreateEnvironmentInPersistentLocation(SPath repoPath, bool enableEnvironmentTrace = false)
        {
            var cacheContainer = new CacheContainer();
            cacheContainer.SetCacheInitializer(CacheType.Branches, () => BranchesCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitAheadBehind, () => GitAheadBehindCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitLocks, () => GitLocksCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitLog, () => GitLogCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitStatus, () => GitStatusCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitUser, () => GitUserCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.RepositoryInfo, () => RepositoryInfoCache.Instance);

            return new IntegrationTestEnvironment(cacheContainer, repoPath, SolutionDirectory,
                new CreateEnvironmentOptions(SPath.SystemTemp.Combine(ApplicationInfo.ApplicationName, "IntegrationTests")),
                enableEnvironmentTrace, false);
        }

        protected IntegrationTestEnvironment CreateCleanEnvironment(ICacheContainer container, SPath repoPath, bool enableEnvironmentTrace = false)
        {
            return new IntegrationTestEnvironment(container,
                repoPath,
                SolutionDirectory,
                new CreateEnvironmentOptions(SPath.CreateTempDirectory("gfu")),
                enableEnvironmentTrace,
                false);
        }

        protected virtual ITaskManager InitializeTaskManager()
        {
            var taskManager = new TaskManager();
            SyncContext = new ThreadSynchronizationContext(taskManager.Token);
            taskManager.Initialize(new SynchronizationContextTaskScheduler(SyncContext));
            return taskManager;
        }
    }
}

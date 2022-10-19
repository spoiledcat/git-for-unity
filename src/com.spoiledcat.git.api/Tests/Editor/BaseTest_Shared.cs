
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Extensions;
using Unity.Editor.Tasks.Helpers;
using Unity.Editor.Tasks.Logging;
using Unity.VersionControl.Git;

namespace BaseTests
{
    using IntegrationTests;
#if NUNIT
    using TestWebServer;
#endif
    using System;
    using System.Threading;
    using Unity.VersionControl.Git.IO;
    using ILogging = Unity.Editor.Tasks.Logging.ILogging;

    internal class TestRepoData
    {
        public TestRepoData(TestData test, string repoName)
        {
            RepoPath = test.TestPath.Combine("IOTestsRepo", repoName);
            DotGitPath = RepoPath.Combine(".git");
            if (DotGitPath.FileExists())
            {
                DotGitPath = DotGitPath.ReadAllLines().Where(x => x.StartsWith("gitdir:"))
                                       .Select(x => x.Substring(7).Trim().ToSPath()).First();
            }
            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
        }

        public readonly SPath RepoPath;
        public readonly SPath DotGitConfig;
        public readonly SPath DotGitHead;
        public readonly SPath DotGitIndex;
        public readonly SPath RemotesPath;
        public readonly SPath BranchesPath;
        public readonly SPath DotGitPath;
    }

    internal class TestData : IDisposable
	{
		public readonly Stopwatch Watch;
		public readonly ILogging Logger;
		public readonly SPath TestPath;
        public readonly SPath SourceDirectory;

        public readonly string TestName;
		public readonly IGitEnvironment Environment;
        public readonly IApplicationManager ApplicationManager;

        public IPlatform Platform => ApplicationManager.Platform;
        public ITaskManager TaskManager => Platform.TaskManager;
        public IProcessManager ProcessManager => Platform.ProcessManager;
        public IProcessEnvironment GitProcessEnvironment => Platform.GitProcessEnvironment;
        public IProcessEnvironment DefaultProcessEnvironment => Platform.DefaultProcessEnvironment;
        public IGitClient GitClient => ApplicationManager.GitClient;
        public IRepository Repository => Environment.Repository;

        public IRepositoryManager RepositoryManager { get; private set; }
#if NUNIT
        public readonly HttpServer HttpServer;
#endif

        public TestRepoData TestRepo { get; private set; }

        public const string TestRepoMasterCleanSynchronized = "IOTestsRepo_master_clean_sync";
        public const string TestRepoMasterCleanUnsynchronized = "IOTestsRepo_master_clean_unsync";
        public const string TestRepoMasterCleanUnsynchronizedRussianLanguage = "IOTestsRepo_master_clean_sync_with_russian_language";
        public const string TestRepoMasterDirtyUnsynchronized = "IOTestsRepo_master_dirty_unsync";
        public const string TestRepoMasterTwoRemotes = "IOTestsRepo_master_two_remotes";
        public const string DefaultExtensionFolder = "ExtensionFolder";
        public const string DefaultUserProfilePath = "UserProfile";
        public const string DefaultUnityProjectPathAndRepositoryPath = "UnityProject";

        private readonly CancellationTokenSource cts;

        public TestData(string testName, ILogging logger, string testRepoName = null, bool withHttpServer = false,
            ICacheContainer cacheContainer = null,
            IFileSystem fileSystem = null)
        {
            TestName = testName;
            Logger = logger;
            Watch = new Stopwatch();
            SourceDirectory = TestContext.CurrentContext.TestDirectory.ToSPath();
            TestPath = SPath.CreateTempDirectory(testName);
            SPath.FileSystem = fileSystem ?? new FileSystem(TestPath);

            if (cacheContainer == null)
            {
                var container = new CacheContainer();
                container.SetCacheInitializer(CacheType.Branches, () => BranchesCache.Instance);
                container.SetCacheInitializer(CacheType.GitAheadBehind, () => GitAheadBehindCache.Instance);
                container.SetCacheInitializer(CacheType.GitLocks, () => GitLocksCache.Instance);
                container.SetCacheInitializer(CacheType.GitLog, () => GitLogCache.Instance);
                container.SetCacheInitializer(CacheType.GitStatus, () => GitStatusCache.Instance);
                container.SetCacheInitializer(CacheType.GitUser, () => GitUserCache.Instance);
                container.SetCacheInitializer(CacheType.RepositoryInfo, () => RepositoryInfoCache.Instance);
                cacheContainer = container;
            }


            Environment = new IntegrationTestEnvironment(cacheContainer, TestPath, TestPath.Parent, testName);
            InitializeEnvironment(testRepoName);

            ApplicationManager = new ApplicationManagerBase(new MainThreadSynchronizationContext(), Environment);

            if (testRepoName != null)
            {
                var testZipFilePath = SourceDirectory.Combine("IOTestsRepo.zip");
                ZipHelper.Instance.Extract(testZipFilePath, TestPath, (_, __) => { }, (value, total, name) => true, token: TaskManager.Token);
                TestRepo = new TestRepoData(this, testRepoName);

                InstallTestGit();
                InitializeRepository();
            }

#if NUNIT
            if (withHttpServer)
            {
                var filesToServePath = SourceDirectory.Combine("files");
                HttpServer = new HttpServer(filesToServePath, 0);
                var started = new ManualResetEventSlim();
                var task = TaskManager.With(HttpServer.Start, TaskAffinity.None);
                task.OnStart += _ => started.Set();
                task.Start();
                started.Wait();
            }
#endif
            ((ApplicationManagerBase)ApplicationManager).Initialize();

            Logger.Trace($"START {testName}");
			Watch.Start();
		}

        public void InitializeRepository()
        {
            RepositoryManager = Unity.VersionControl.Git.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, TestRepo.RepoPath);
            RepositoryManager.Initialize();

            Environment.InitializeRepository(TestRepo.RepoPath);
            Repository.Initialize(RepositoryManager, TaskManager);

            RepositoryManager.Start();
            Repository.Start();
        }

        public void InstallTestGit()
        {
            var installDetails = Environment.GitDefaultInstallation;
            var state = installDetails.GetDefaults();
            Environment.GitInstallationState = state;

            if (installDetails.GitExecutablePath.FileExists() && installDetails.GitLfsExecutablePath.FileExists())
                return;

            var key = installDetails.GitManifest.FileNameWithoutExtension + "_updatelastCheckTime";
            Environment.UserSettings.Set(key, DateTimeOffset.Now);

            var localCache = SourceDirectory.Combine("files/git");
            localCache.CopyFiles(installDetails.ZipPath.Parent, true);
            // skip checking for updates

            state.GitPackage = DugiteReleaseManifest.Load(TaskManager, installDetails.GitManifest,
                GitInstaller.GitInstallDetails.ManifestFeed, Environment);
            var asset = state.GitPackage.DugitePackage;
            state.GitZipPath = installDetails.ZipPath.Combine(asset.Name);

            installDetails.GitInstallationPath.DeleteIfExists();

            state.GitZipPath.EnsureParentDirectoryExists();

            var gitExtractPath = TestPath.Combine("setup", "git_zip_extract_zip_paths").EnsureDirectoryExists();
            var source = new UnzipTask(TaskManager, state.GitZipPath, gitExtractPath)
                .RunSynchronously();

            installDetails.GitInstallationPath.EnsureParentDirectoryExists();
            source.Move(installDetails.GitInstallationPath);
        }


        private void InitializeEnvironment(string testRepoName)
		{
            var projectPath = TestPath.Combine(DefaultUnityProjectPathAndRepositoryPath);

            if (testRepoName != null)
            {
                projectPath = TestPath.Combine("IOTestsRepo", testRepoName);
            }

#if UNITY_EDITOR
			Environment.Initialize(SPath.Default, projectPath, TheEnvironment.instance.Environment.UnityVersion, TheEnvironment.instance.Environment.UnityApplication, TheEnvironment.instance.Environment.UnityApplicationContents);
			return;
#endif

			SPath unityPath, unityContentsPath;
			unityPath = CurrentExecutionDirectory;

			while (!unityPath.IsEmpty && !unityPath.DirectoryExists(".Editor"))
				unityPath = unityPath.Parent;

			if (!unityPath.IsEmpty)
			{
				unityPath = unityPath.Combine(".Editor");
				unityContentsPath = unityPath.Combine("Data");
			}
			else
			{
				unityPath = unityContentsPath = SPath.Default;
			}

			Environment.Initialize(SPath.Default, projectPath, "2019.2", unityPath, unityContentsPath);
		}

		public void Dispose()
		{
			Watch.Stop();

#if NUNIT
            try
            {
                if (HttpServer != null)
                {
                    HttpServer.Stop();
                }
            } catch { }
#endif
            ProcessManager.Dispose();
			if (SynchronizationContext.Current is IMainThreadSynchronizationContext ourContext)
				ourContext.Dispose();

			TaskManager.Dispose();

            //TestPath.Delete();
			Logger.Trace($"STOP {TestName} :{Watch.ElapsedMilliseconds}ms");
		}

		internal SPath CurrentExecutionDirectory => System.Reflection.Assembly.GetExecutingAssembly().Location.ToSPath().Parent;
	}

	public partial class BaseTest
	{
		protected const int Timeout = 5000;
		protected const int RandomSeed = 120938;

		protected void StartTrackTime(Stopwatch watch, ILogging logger, string message = "")
		{
			if (!string.IsNullOrEmpty(message))
				logger.Trace(message);
			watch.Reset();
			watch.Start();
		}

		protected void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
		{
			watch.Stop();
			logger.Trace($"Time: {watch.ElapsedMilliseconds}");
		}

		protected ActionTask GetTask(ITaskManager taskManager, TaskAffinity affinity, int id, Action<int> body)
		{
			return new ActionTask(taskManager, _ => body(id)) { Affinity = affinity };
		}

		protected static IEnumerable<object> StartAndWaitForCompletion(params ITask[] tasks)
		{
			foreach (var task in tasks) task.Start();
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> StartAndWaitForCompletion(IEnumerable<ITask> tasks)
		{
			foreach (var task in tasks) task.Start();
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(params ITask[] tasks)
		{
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(IEnumerable<ITask> tasks)
		{
			while (!tasks.All(x => x.Task.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(IEnumerable<Task> tasks)
		{
			while (!tasks.All(x => x.IsCompleted)) yield return null;
		}

		protected static IEnumerable<object> WaitForCompletion(params Task[] tasks)
		{
			while (!tasks.All(x => x.IsCompleted)) yield return null;
		}
	}


	internal static class TestExtensions
	{
        public static void Matches(this GitBranch branch, GitBranch other)
        {
            Assert.AreEqual(other, branch);
        }

        public static void Matches(this GitRemote branch, GitRemote other)
        {
            Assert.AreEqual(other, branch);
        }

        public static void Matches(this GitStatus branch, GitStatus other)
        {
            Assert.AreEqual(other, branch);
        }

        public static void Matches(this IEnumerable actual, IEnumerable expected)
		{
			CollectionAssert.AreEqual(expected, actual, $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void Matches<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
		{
			CollectionAssert.AreEqual(expected.ToArray(), actual.ToArray(), $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void MatchesUnsorted(this IEnumerable actual, IEnumerable expected)
		{
			CollectionAssert.AreEquivalent(expected, actual, $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void MatchesUnsorted<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
		{
			CollectionAssert.AreEquivalent(expected.ToArray(), actual.ToArray(), $"{Environment.NewLine}expected:{expected.Join()}{Environment.NewLine}actual  :{actual.Join()}{Environment.NewLine}");
		}

		public static void Matches(this string actual, string expected) => Assert.AreEqual(expected, actual);
		public static void Matches(this int actual, int expected) => Assert.AreEqual(expected, actual);
		public static void Matches(this SPath actual, SPath expected) => Assert.AreEqual(expected, actual);

        public static UriString FixPort(this UriString url, int port)
        {
            var uri = url.ToUri();
            return UriString.TryParse(new UriBuilder(uri.Scheme, uri.Host, port, uri.PathAndQuery).Uri.ToString());
        }
	}

	static class KeyValuePair
	{
		public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
		{
			return new KeyValuePair<TKey, TValue>(key, value);
		}
	}

}

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Unity.VersionControl.Git;
using NSubstitute;
using NUnit.Framework;
using System.IO;
using System.Linq;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class CleanGitInstallerTests : BaseTestWithHttpServer
    {
        [Test]
        public void NoLocalGit_NoDownload_DoesntThrow()
        {
            var cacheContainer = Substitute.For<ICacheContainer>();
            var environment = CreateCleanEnvironment(cacheContainer, TestBasePath, true);
            var platform = new Platform(environment);
            var taskManager = InitializeTaskManager();
            var processManager = new ProcessManager(environment, platform.GitEnvironment, taskManager.Token);

            GitInstaller.GitInstallDetails.GitPackageFeed = "fail";

            var currentState = environment.GitDefaultInstallation.GetDefaults();
            var gitInstaller = new GitInstaller(environment, processManager, taskManager.Token, currentState);

            var newState = gitInstaller.RunSynchronously();
            Assert.AreEqual(currentState, newState);
        }
    }

    [TestFixture]
    class GitInstallerTests : BaseIntegrationTest
    {
        const int Timeout = 30000;
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, false, false);
            InitializePlatform(TestBasePath);
        }

        private TestWebServer.HttpServer server;
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"), 50000);
            Task.Factory.StartNew(server.Start);
            ApplicationConfiguration.WebTimeout = 10000;
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
            ZipHelper.Instance = null;
        }

        [Test]
        public void GitInstallWindows()
        {
            var gitInstallationPath = TestBasePath.Combine("GitInstall").CreateDirectory();

            GitInstaller.GitInstallDetails.GitPackageFeed =
                $"http://localhost:{server.Port}/{GitInstaller.GitInstallDetails.GitPackageName}";

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, Environment);

            TestBasePath.Combine("git").CreateDirectory();

            var zipHelper = Substitute.For<IZipHelper>();
            zipHelper.Extract(Arg.Any<string>(), Arg.Do<string>(x =>
            {
                var n = x.ToNPath();
                n.EnsureDirectoryExists();
                if (n.FileName == "git")
                {
                    n.Combine("git" + Environment.ExecutableExtension).WriteAllText("");
                }
            }), Arg.Any<CancellationToken>(), Arg.Any<Action<string, long>>(), Arg.Any<Func<long, long, string, bool>>()).Returns(true);
            ZipHelper.Instance = zipHelper;
            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager.Token, installDetails: installDetails);

            var state = gitInstaller.RunSynchronously();
            state.Should().NotBeNull();

            Assert.AreEqual(gitInstallationPath.Combine(GitInstaller.GitInstallDetails.GitDirectory), state.GitInstallationPath);
            state.GitExecutablePath.Should().Be(gitInstallationPath.Combine(GitInstaller.GitInstallDetails.GitDirectory, "cmd", "git" + Environment.ExecutableExtension));

            Environment.GitInstallationState = state;

            var procTask = new SimpleProcessTask(TaskManager.Token, "something")
                .Configure(ProcessManager);
            procTask.Process.StartInfo.EnvironmentVariables["PATH"].Should().StartWith(gitInstallationPath.ToString());
        }

        //[Test]
        public void MacSkipsInstallWhenSettingsGitExists()
        {
            DefaultEnvironment.OnMac = true;
            DefaultEnvironment.OnWindows = false;

            var filesystem = Substitute.For<IFileSystem>();
            filesystem.FileExists(Arg.Any<string>()).Returns(true);
            filesystem.DirectoryExists(Arg.Any<string>()).Returns(true);
            filesystem.DirectorySeparatorChar.Returns('/');
            Environment.FileSystem = filesystem;

            var gitInstallationPath = "/usr/local".ToNPath();
            var gitExecutablePath = gitInstallationPath.Combine("bin/git");
            var gitLfsInstallationPath = gitInstallationPath;
            var gitLfsExecutablePath = gitLfsInstallationPath.Combine("bin/git-lfs");

            GitInstaller.GitInstallDetails.GitPackageFeed =
                $"http://localhost:{server.Port}/unity/git/mac/{GitInstaller.GitInstallDetails.GitPackageName}";

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, Environment);

            var ret = new string[] { gitLfsExecutablePath };
            filesystem.GetFiles(Arg.Any<string>(), Arg.Is<string>(installDetails.GitLfsExecutablePath.FileName), Arg.Any<SearchOption>())
                      .Returns(ret);

            var settings = Substitute.For<ISettings>();
            var settingsRet = gitExecutablePath.ToString();
            settings.Get(Arg.Is<string>(Constants.GitInstallPathKey), Arg.Any<string>()).Returns(settingsRet);
            var installer = new GitInstaller(Environment, ProcessManager, TaskManager.Token, installDetails: installDetails);

            var result = installer.RunSynchronously();
            Assert.AreEqual(gitInstallationPath, result.GitInstallationPath);
            Assert.AreEqual(gitLfsInstallationPath, result.GitLfsInstallationPath);
            Assert.AreEqual(gitExecutablePath, result.GitExecutablePath);
            Assert.AreEqual(gitLfsExecutablePath, result.GitLfsExecutablePath);
        }
    }

    [TestFixture]
    class GitInstallerTestsWithHttp : BaseIntegrationTestWithHttpServer
    {
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, false, false);
            InitializePlatform(TestBasePath);
        }

        [Test]
        public void GitIsInstalledIfMissing()
        {
            GitInstaller.GitInstallDetails.GitPackageFeed = $"http://localhost:{server.Port}/{GitInstaller.GitInstallDetails.GitPackageName}";
            var installDetails = new GitInstaller.GitInstallDetails(TestBasePath, Environment);
            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager.Token, installDetails: installDetails);
            var result = gitInstaller.RunSynchronously();
            result.Should().NotBeNull();

            var expectedInstallationPath = TestBasePath.Combine("Git");
            Assert.AreEqual(expectedInstallationPath, result.GitInstallationPath);
            result.GitExecutablePath.Should().Be(expectedInstallationPath.Combine("cmd", "git" + Environment.ExecutableExtension));
        }
    }
}

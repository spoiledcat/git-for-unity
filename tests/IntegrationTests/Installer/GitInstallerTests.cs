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
using BaseTests;
using TestUtils;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;
using Unity.VersionControl.Git.IO;

namespace IntegrationTests
{
    [TestFixture]
    class GitInstallerTests : BaseTest
    {
        [Test]
        public void NoLocalGit_NoDownload_DoesntThrow()
        {
            var cacheContainer = Substitute.For<ICacheContainer>();
            using (var test = StartTest(cacheContainer: cacheContainer))
            {
                var manifestFeed = "fail";

                var currentState = test.Environment.GitDefaultInstallation.GetDefaults();
                var gitInstaller = new GitInstaller(test.Platform, currentState,
                    new GitInstaller.GitInstallDetails(test.TestPath, test.Environment)
                        { GitManifestFeed = manifestFeed }
                );

                var newState = gitInstaller.RunSynchronously();
                Assert.AreEqual(currentState, newState);
            }
        }

        [Test]
        public void GitInstallWindows()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var gitInstallationPath = test.TestPath.Combine("GitInstall").CreateDirectory();

                var manifestFeed = $"http://localhost:{test.HttpServer.Port}/git/{GitInstaller.GitInstallDetails.ManifestName}";
                var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, test.Environment)
                    { GitManifestFeed = manifestFeed };

                test.TestPath.Combine("git").CreateDirectory();

                var gitInstaller = new GitInstaller(test.Platform, installDetails: installDetails);

                var zipHelper = Substitute.For<IZipHelper>();
                zipHelper.Extract(Arg.Any<string>(), Arg.Do<string>(x =>
                {
                    var n = x.ToSPath();
                    n.EnsureDirectoryExists();
                    if (n.FileName == "git")
                    {
                        n.Combine("git" + test.Environment.ExecutableExtension).WriteAllText("");
                    }
                }), Arg.Any<Action<string, long>>(), Arg.Any<Func<long, long, string, bool>>(), Arg.Any<Func<string, bool>>(), Arg.Any<CancellationToken>()).Returns(true);

                GitInstaller.GitInstallationState state = null;
                try
                {
                    ZipHelper.Instance = zipHelper;
                    state = gitInstaller.RunSynchronously();
                }
                finally
                {
                    ZipHelper.Instance = null;
                }

                state.Should().NotBeNull();

                Assert.AreEqual(gitInstallationPath.Combine(GitInstaller.GitInstallDetails.GitDirectory), state.GitInstallationPath);
                state.GitExecutablePath.Should().Be(gitInstallationPath.Combine(GitInstaller.GitInstallDetails.GitDirectory,
                    "cmd", "git" + test.Environment.ExecutableExtension));

                test.Environment.GitInstallationState = state;

                var procTask = new NativeProcessTask(test.TaskManager, test.ProcessManager, test.GitProcessEnvironment, "something", null);
                procTask.Wrapper.StartInfo.EnvironmentVariables["PATH"].Should().StartWith(gitInstallationPath.ToString());
            }
        }

        [Test]
        public void GitIsInstalledIfMissing()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var manifestFeed = $"http://localhost:{test.HttpServer.Port}/git/{GitInstaller.GitInstallDetails.ManifestName}";
                var installDetails = new GitInstaller.GitInstallDetails(test.TestPath, test.Environment)
                    { GitManifestFeed = manifestFeed };
                var gitInstaller = new GitInstaller(test.Platform, installDetails: installDetails);
                var result = gitInstaller.RunSynchronously();
                result.Should().NotBeNull();

                var expectedInstallationPath = test.TestPath.Combine("Git");
                Assert.AreEqual(expectedInstallationPath, result.GitInstallationPath);
                result.GitExecutablePath.Should().Be(expectedInstallationPath.Combine("cmd", "git" + test.Environment.ExecutableExtension));
            }
        }

        [Test]
        public void GitNotInstalledIfUpToDate()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var manifestFeed = $"http://localhost:{test.HttpServer.Port}/git/{GitInstaller.GitInstallDetails.ManifestName}";
                var installDetails = new GitInstaller.GitInstallDetails(test.TestPath, test.Environment)
                    { GitManifestFeed = manifestFeed };
                var gitInstaller = new GitInstaller(test.Platform, installDetails: installDetails);
                var result = gitInstaller.RunSynchronously();
                result.Should().NotBeNull();

                var expectedInstallationPath = test.TestPath.Combine("Git");
                Assert.AreEqual(expectedInstallationPath, result.GitInstallationPath);
                result.GitExecutablePath.Should().Be(expectedInstallationPath.Combine("cmd", "git" + test.Environment.ExecutableExtension));

                test.Environment.GitInstallationState = result;

                gitInstaller = new GitInstaller(test.Platform, installDetails: installDetails);
                var state = gitInstaller.VerifyGitSettings();
                Assert.True(state.GitIsValid);
                Assert.True(state.GitLfsIsValid);
            }
        }
    }
}

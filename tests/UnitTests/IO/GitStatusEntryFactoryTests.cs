using FluentAssertions;
using NUnit.Framework;
using Unity.VersionControl.Git;
using NCrunch.Framework;
using NSubstitute.Core;
using TestUtils;
using Unity.VersionControl.Git.IO;

namespace UnitTests
{
    [TestFixture, Isolated]
    class GitStatusEntryFactoryTests
    {
        protected TestSubstituteFactory SubstituteFactory { get; private set; }

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new TestSubstituteFactory();
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInGitRoot()
        {
            var repositoryPath = "/Source".ToSPath();
            var unityProjectPath = repositoryPath.Combine("UnityProject");

            SubstituteFactory.CreateProcessEnvironment(repositoryPath);
            var environment = SubstituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                RepositoryPath = repositoryPath,
                UnityProjectPath = unityProjectPath
            });

            SPath.FileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                CurrentDirectory = repositoryPath
            });

            const string inputPath = "Something.sln";
            const GitFileStatus inputStatus = GitFileStatus.Added;

            var expectedFullPath = repositoryPath.Combine(inputPath);
            var expectedProjectPath = expectedFullPath.RelativeTo(unityProjectPath);

            var expected = new GitStatusEntry(inputPath, expectedFullPath, expectedProjectPath, GitFileStatus.None, inputStatus);

            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(inputPath, GitFileStatus.None, inputStatus);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsChildOfGitRootAndFileInProjectRoot()
        {
            var repositoryPath = "/Source".ToSPath();
            var unityProjectPath = repositoryPath.Combine("UnityProject");

            SubstituteFactory.CreateProcessEnvironment(repositoryPath);
            var environment = SubstituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                RepositoryPath = repositoryPath,
                UnityProjectPath = unityProjectPath
            });
            SPath.FileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                CurrentDirectory = repositoryPath
            });

            var inputPath = "UnityProject/Something.sln".ToSPath().ToString();
            const GitFileStatus inputStatus = GitFileStatus.Added;

            var expectedFullPath = repositoryPath.Combine(inputPath);
            const string expectedProjectPath = "Something.sln";

            var expected = new GitStatusEntry(inputPath, expectedFullPath, expectedProjectPath, GitFileStatus.None, inputStatus);

            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(inputPath, GitFileStatus.None, inputStatus);

            result.Should().Be(expected);
        }

        [Test]
        public void CreateObjectWhenProjectRootIsSameAsGitRootAndFileInGitRoot()
        {
            var repositoryPath = "/Source".ToSPath();
            var unityProjectPath = repositoryPath;

            SubstituteFactory.CreateProcessEnvironment(repositoryPath);
            var environment = SubstituteFactory.CreateEnvironment(new CreateEnvironmentOptions {
                RepositoryPath = repositoryPath,
                UnityProjectPath = unityProjectPath
            });
            SPath.FileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions {
                CurrentDirectory = repositoryPath
            });

            const string inputPath = "Something.sln";
            const GitFileStatus inputStatus = GitFileStatus.Added;

            var expectedFullPath = repositoryPath.Combine(inputPath);
            const string expectedProjectPath = inputPath;

            var expected = new GitStatusEntry(inputPath, expectedFullPath, expectedProjectPath, GitFileStatus.None, inputStatus);

            var gitStatusEntryFactory = new GitObjectFactory(environment);

            var result = gitStatusEntryFactory.CreateGitStatusEntry(inputPath, GitFileStatus.None, inputStatus);

            result.Should().Be(expected);
        }
    }
}

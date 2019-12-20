using System;
using FluentAssertions;
using Unity.VersionControl.Git;
using NCrunch.Framework;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using TestUtils;
using Unity.VersionControl.Git.IO;

namespace UnitTests
{
    [TestFixture, Isolated]
    public class EnvironmentExtensionTests
    {
        private TestSubstituteFactory SubstituteFactory { get; } = new TestSubstituteFactory();

        [SetUp]
        public void TestSetup()
        {
            var fileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions());
            SPath.FileSystem = fileSystem;
        }

        [TearDown]
        public void TestTearDown()
        {
            SPath.FileSystem = null;
        }

        [Test, Sequential]
        public void GetRepositoryPathReturnsRelativePathToRepository(
            [Values(@"c:\UnityProject", "/Projects", @"c:\UnityProject")] string repositoryPath,
            [Values(@"c:\UnityProject", "/Projects/UnityProject", "c:/UnityProject")]string projectPath,
            [Values(@"test.txt", "test.txt", "test.txt")]string path,
            [Values(@"test.txt", "UnityProject/test.txt", "test.txt")]string expected)
        {
            var environment = Substitute.For<IGitEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath.ToSPath());
            environment.UnityProjectPath.Returns(projectPath);

            SPath nExpected = expected.ToSPath();
            var repositoryFilePath = path.ToSPath().RelativeToRepository(environment);
            repositoryFilePath.Should().Be(nExpected);
        }

        [Test, Sequential]
        public void GetRepositoryPathThrowsWhenRepositoryIsChildOfProject(
            [Values(@"c:\UnityProject\UnityProject\Assets")] string repositoryPath,
            [Values(@"c:\UnityProject\UnityProject")]string projectPath,
            [Values(@"test.txt")]string path)
        {
            var environment = Substitute.For<IGitEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath.ToSPath());
            environment.UnityProjectPath.Returns(projectPath);

            Action act = () => path.ToSPath().RelativeToRepository(environment);
            act.Should().Throw<InvalidOperationException>();
        }

        [Test, Sequential]
        public void GetAssetPathReturnsRelativePathToProject(
            [Values(@"c:\Projects", "/Projects", "/UnityProject")] string repositoryPath,
            [Values(@"c:\Projects\UnityProject", "/Projects/Unity/UnityProject", "/UnityProject")] string projectPath,
            [Values(@"UnityProject\test.txt", "Unity/UnityProject/Assets/test.txt", "test.txt")] string path,
            [Values("test.txt", "Assets/test.txt", "test.txt")] string expected)
        {
            var environment = Substitute.For<IGitEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath.ToSPath());
            environment.UnityProjectPath.Returns(projectPath);

            SPath nExpected = expected.ToSPath();
            var repositoryFilePath = path.ToSPath().RelativeToProject(environment);
            repositoryFilePath.Should().Be(nExpected);
        }

        [Test]
        public void GetAssetPathShouldThrowWhenRepositoryRootIsChild(
            [Values(@"c:\Projects\UnityProject\Assets")] string repositoryPath,
            [Values(@"c:\Projects\UnityProject")] string projectPath,
            [Values("test.txt")] string path)
        {
            var environment = Substitute.For<IGitEnvironment>();
            environment.RepositoryPath.Returns(repositoryPath.ToSPath());
            environment.UnityProjectPath.Returns(projectPath);

            Action act = () => path.ToSPath().RelativeToProject(environment);
            act.Should().Throw<InvalidOperationException>();
        }
    }
}

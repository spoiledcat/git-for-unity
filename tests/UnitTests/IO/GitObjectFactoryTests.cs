using Unity.VersionControl.Git;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture, Isolated]
    class GitObjectFactoryTests
    {
        private static readonly TestSubstituteFactory SubstituteFactory = new TestSubstituteFactory();

        [Test]
        public void ShouldParseNormalFile()
        {
            SPath.FileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions() {
                CurrentDirectory = @"c:\Projects\UnityProject"
            });

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(@"c:\Projects\UnityProject".ToSPath());
            environment.UnityProjectPath.Returns(@"c:\Projects\UnityProject".ToSPath());

            var gitObjectFactory = new GitObjectFactory(environment);
            var gitStatusEntry = gitObjectFactory.CreateGitStatusEntry("hello.txt", GitFileStatus.None, GitFileStatus.Deleted);

            Assert.AreEqual(@"c:\Projects\UnityProject\hello.txt", gitStatusEntry.FullPath);
        }


        [Test]
        public void ShouldParseOddFile()
        {
            SPath.FileSystem = SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions()
            {
                CurrentDirectory = @"c:\Projects\UnityProject"
            });

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(@"c:\Projects\UnityProject".ToSPath());
            environment.UnityProjectPath.Returns(@"c:\Projects\UnityProject".ToSPath());

            var gitObjectFactory = new GitObjectFactory(environment);
            var gitStatusEntry = gitObjectFactory.CreateGitStatusEntry("c:UsersOculusGoVideo.mp4", GitFileStatus.None, GitFileStatus.Deleted);

            Assert.AreEqual(@"c:\Projects\UnityProject\c:UsersOculusGoVideo.mp4", gitStatusEntry.FullPath);
        }
    }
}

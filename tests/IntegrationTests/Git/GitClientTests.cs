using System.Collections.Generic;
using BaseTests;
using FluentAssertions;
using Unity.VersionControl.Git;
using NUnit.Framework;
using Unity.VersionControl.Git.IO;

namespace IntegrationTests
{
    [TestFixture]
    class A_GitClientTests : BaseTest
    {
        readonly string[] m_CleanFiles = { "file1.txt", "file2.txt", "file3.txt" };

        [Test]
        public void AaSetupGitFirst()
        {
            // empty test that sets up git for the rest of the tests
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {}
        }

        [Test]
        public void ShouldGetGitVersion()
        {
            if (!SPath.IsWindows)
                return;

            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {

                var result = test.GitClient.Version().RunSynchronously();
                var expected = TheVersion.Parse("2.21.0");
                result.Major.Should().Be(expected.Major);
                result.Minor.Should().Be(expected.Minor);
                result.Patch.Should().Be(expected.Patch);
            }
        }

        [Test]
        public void ShouldGetGitLfsVersion()
        {
            if (!SPath.IsWindows)
                return;

            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                var result = test.GitClient.LfsVersion().RunSynchronously();
                var expected = TheVersion.Parse("2.6.1");
                result.Should().Be(expected);
            }
        }

        [Test]
        public void ShouldCleanFile()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                foreach (var file in m_CleanFiles)
                {
                    file.ToSPath().WriteAllText("Some test text.");
                }

                test.GitClient.Clean(new List<string> { m_CleanFiles[0], m_CleanFiles[2] }).RunSynchronously();

                m_CleanFiles[0].ToSPath().Exists().Should().BeFalse();
                m_CleanFiles[1].ToSPath().Exists().Should().BeTrue();
                m_CleanFiles[2].ToSPath().Exists().Should().BeFalse();
            }
        }

        [Test]
        public void ShouldCleanAllFiles()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                foreach (var file in m_CleanFiles)
                {
                    file.ToSPath().WriteAllText("Some test text.");
                }

                test.GitClient.CleanAll().RunSynchronously();

                m_CleanFiles[0].ToSPath().Exists().Should().BeFalse();
                m_CleanFiles[1].ToSPath().Exists().Should().BeFalse();
                m_CleanFiles[2].ToSPath().Exists().Should().BeFalse();
            }
        }

        [Test]
        public void ShouldResetRepositoryState()
        {
            using (var test = StartTest(TestData.TestRepoMasterCleanSynchronized))
            {
                const string initialFile = "init-file.txt";
                const string testFile = "reset-file.txt";

                // Initial commit
                initialFile.ToSPath().WriteAllText("Some test text.");
                test.GitClient.Add(new List<string> { initialFile }).RunSynchronously();
                test.GitClient.Commit("initial", "commit").RunSynchronously();

                // Add file
                testFile.ToSPath().WriteAllText("Some test text.");
                test.GitClient.Add(new List<string> { testFile }).RunSynchronously();
                test.GitClient.Commit("test", "commit").RunSynchronously();
                testFile.ToSPath().Exists().Should().BeTrue();

                // Reset to commit without file
                test.GitClient.Reset("HEAD~1", GitResetMode.Hard).RunSynchronously();
                testFile.ToSPath().Exists().Should().BeFalse();
            }
        }
    }
}

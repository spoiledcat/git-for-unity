using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Unity.VersionControl.Git;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class A_GitClientTests : BaseGitTestWithHttpServer
    {
        protected override int Timeout { get; set; } = 5 * 60 * 1000;

        readonly string[] m_CleanFiles = { "file1.txt", "file2.txt", "file3.txt" };

        [Test]
        public void AaSetupGitFirst()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        }

        [Test]
        public void ShouldGetGitVersion()
        {
            if (!DefaultEnvironment.OnWindows)
                return;

            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            var result = GitClient.Version().RunSynchronously();
            var expected = TheVersion.Parse("2.21.0");
            result.Major.Should().Be(expected.Major);
            result.Minor.Should().Be(expected.Minor);
            result.Patch.Should().Be(expected.Patch);
        }

        [Test]
        public void ShouldGetGitLfsVersion()
        {
            if (!DefaultEnvironment.OnWindows)
                return;

            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            var result = GitClient.LfsVersion().RunSynchronously();
            var expected = TheVersion.Parse("2.6.1");
            result.Should().Be(expected);
        }

        [Test]
        public void ShouldCleanFile()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            foreach (var file in m_CleanFiles)
            {
                file.ToNPath().WriteAllText("Some test text.");
            }

            GitClient.Clean(new List<string> { m_CleanFiles[0], m_CleanFiles[2] }).RunSynchronously();

            File.Exists(m_CleanFiles[0]).Should().BeFalse();
            File.Exists(m_CleanFiles[1]).Should().BeTrue();
            File.Exists(m_CleanFiles[2]).Should().BeFalse();
        }

        [Test]
        public void ShouldCleanAllFiles()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            foreach (var file in m_CleanFiles)
            {
                file.ToNPath().WriteAllText("Some test text.");
            }

            GitClient.CleanAll().RunSynchronously();

            File.Exists(m_CleanFiles[0]).Should().BeFalse();
            File.Exists(m_CleanFiles[1]).Should().BeFalse();
            File.Exists(m_CleanFiles[2]).Should().BeFalse();
        }
    }
}

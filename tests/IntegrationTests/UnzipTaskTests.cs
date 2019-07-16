using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Unity.VersionControl.Git;
using Microsoft.Win32.SafeHandles;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class UnzipTaskTests : BaseIntegrationTest
    {
        [Test]
        public async Task UnzipWorks()
        {
            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory, new CreateEnvironmentOptions(TestBasePath));
            InitializeTaskManager();

            var expectedContent = @"Yup this is

{
  good énough
}
".Replace("\r\n", "\n");

            var destinationPath = TestBasePath.Combine("unziptests").CreateDirectory();
            var localCache = TestLocation.Combine("UnzipTestResources");
            var archiveFilePath = localCache.Combine("testfile.zip");
            var extractedPath = TestBasePath.Combine("zipextract").CreateDirectory();

            var unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath,
                    ZipHelper.Instance,
                    Environment.FileSystem);

            await unzipTask.StartAwait();

            var expectedFile = extractedPath.Combine("embedded-git.json");
            expectedFile.Parent.DirectoryExists().Should().BeTrue();
            expectedFile.FileExists().Should().BeTrue();
            var actualContent = expectedFile.ReadAllText();
            actualContent.Should().Be(expectedContent);

            extractedPath = TestBasePath.Combine("tgzextract").CreateDirectory();
            archiveFilePath = localCache.Combine("testfile.tgz");

            unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath,
                ZipHelper.Instance,
                Environment.FileSystem);

            await unzipTask.StartAwait();

            expectedFile = extractedPath.Combine("embedded-git.json");
            expectedFile.Parent.DirectoryExists().Should().BeTrue();
            expectedFile.FileExists().Should().BeTrue();
            expectedFile.ReadAllText().Should().Be(expectedContent);

            extractedPath = TestBasePath.Combine("targzextract").CreateDirectory();
            archiveFilePath = localCache.Combine("testfile.tar.gz");

            unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath,
                ZipHelper.Instance,
                Environment.FileSystem);

            await unzipTask.StartAwait();

            expectedFile = extractedPath.Combine("embedded-git.json");
            expectedFile.Parent.DirectoryExists().Should().BeTrue();
            expectedFile.FileExists().Should().BeTrue();
            expectedFile.ReadAllText().Should().Be(expectedContent);
        }
    }
}

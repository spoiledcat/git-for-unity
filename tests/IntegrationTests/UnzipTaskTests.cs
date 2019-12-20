using System.Threading.Tasks;
using BaseTests;
using FluentAssertions;
using Unity.VersionControl.Git;
using NSubstitute;
using NUnit.Framework;
using Unity.Editor.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    class UnzipTaskTests : BaseTest
    {
        [Test]
        public async Task UnzipWorks()
        {
            var cacheContainer = Substitute.For<ICacheContainer>();
            using (var test = StartTest(cacheContainer: cacheContainer))
            {

                var expectedContent = @"Yup this is

{
  good énough
}
".Replace("\r\n", "\n");

                var destinationPath = test.TestPath.Combine("unziptests").CreateDirectory();
                var localCache = test.SourceDirectory.Combine("UnzipTestResources");
                var archiveFilePath = localCache.Combine("testfile.zip");
                var extractedPath = test.TestPath.Combine("zipextract").CreateDirectory();

                var unzipTask = new UnzipTask(test.TaskManager, archiveFilePath, extractedPath, ZipHelper.Instance);

                await unzipTask.StartAwait();

                var expectedFile = extractedPath.Combine("embedded-git.json");
                expectedFile.Parent.DirectoryExists().Should().BeTrue();
                expectedFile.FileExists().Should().BeTrue();
                var actualContent = expectedFile.ReadAllText();
                actualContent.Should().Be(expectedContent);

                extractedPath = test.TestPath.Combine("tgzextract").CreateDirectory();
                archiveFilePath = localCache.Combine("testfile.tgz");

                unzipTask = new UnzipTask(test.TaskManager, archiveFilePath, extractedPath, ZipHelper.Instance);

                await unzipTask.StartAwait();

                expectedFile = extractedPath.Combine("embedded-git.json");
                expectedFile.Parent.DirectoryExists().Should().BeTrue();
                expectedFile.FileExists().Should().BeTrue();
                expectedFile.ReadAllText().Should().Be(expectedContent);

                extractedPath = test.TestPath.Combine("targzextract").CreateDirectory();
                archiveFilePath = localCache.Combine("testfile.tar.gz");

                unzipTask = new UnzipTask(test.TaskManager, archiveFilePath, extractedPath, ZipHelper.Instance);

                await unzipTask.StartAwait();

                expectedFile = extractedPath.Combine("embedded-git.json");
                expectedFile.Parent.DirectoryExists().Should().BeTrue();
                expectedFile.FileExists().Should().BeTrue();
                expectedFile.ReadAllText().Should().Be(expectedContent);
            }
        }
    }
}

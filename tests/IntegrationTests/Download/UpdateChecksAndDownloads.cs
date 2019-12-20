using System.Threading.Tasks;
using BaseTests;
using NUnit.Framework;
using Unity.Editor.Tasks;
using Unity.Editor.Tasks.Helpers;
using Unity.VersionControl.Git;
using Unity.VersionControl.Git.IO;

namespace IntegrationTests
{
    class UpdateChecksAndDownloads : BaseTest
    {
#if NUNIT
        [Test]
        public async Task DownloadAndVerificationWorks()
        {
            using (var test = StartTest(withHttpServer: true))
            {
                var file = "unity/latest.json";
                var package = Package.Load(test.TaskManager, test.Environment, new UriString($"http://localhost:{test.HttpServer.Port}/{file}"));

                var downloader = new Downloader(test.TaskManager);
                downloader.QueueDownload(package.Uri.FixPort(test.HttpServer.Port), test.TestPath);

                var task = await Task.WhenAny(downloader.Start().Task, Task.Delay(Timeout));

                Assert.AreEqual(downloader.Task, task);
                Assert.IsTrue(downloader.Successful);
                var result = await downloader.Task;

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual(test.SourceDirectory.Combine("files/unity/releases/github-for-unity-99.2.0-beta1.unitypackage").CalculateMD5(), result[0].File.ToSPath().CalculateMD5());
            }
        }
#endif
    }
}

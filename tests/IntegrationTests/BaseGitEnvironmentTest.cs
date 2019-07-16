using Unity.VersionControl.Git;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseIntegrationTest
    {
        public override void OnSetup()
        {
            base.OnSetup();
            Logger.Trace($"Extracting {TestZipFilePath} to {TestBasePath}");
            ZipHelper.Instance.Extract(TestZipFilePath, TestBasePath.ToString(), TaskManager.Token, (_, __) => { }, (value, total, name) => true);
        }

        public override void OnTearDown()
        {
            RepositoryManager?.Stop();
            RepositoryManager?.Dispose();
            RepositoryManager = null;
            base.OnTearDown();
        }
    }
}

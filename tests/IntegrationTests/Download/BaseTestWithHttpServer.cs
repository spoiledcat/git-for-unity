using System.Threading.Tasks;
using Unity.VersionControl.Git;

namespace IntegrationTests
{
    class BaseTestWithHttpServer : BaseTest
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            ApplicationConfiguration.WebTimeout = 50000;
            var filesToServePath = SolutionDirectory.Combine("files");
            server = new TestWebServer.HttpServer(filesToServePath, 50000);
            Task.Factory.StartNew(server.Start);
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }


    class BaseIntegrationTestWithHttpServer : BaseIntegrationTest
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            ApplicationConfiguration.WebTimeout = 50000;
            var filesToServePath = SolutionDirectory.Combine("files");
            server = new TestWebServer.HttpServer(filesToServePath, 50000);
            Task.Factory.StartNew(server.Start);
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }

    class BaseGitTestWithHttpServer : BaseGitEnvironmentTest
    {
        protected virtual int Timeout { get; set; } = 30 * 1000;
        protected TestWebServer.HttpServer server;

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            ApplicationConfiguration.WebTimeout = 50000;
            var filesToServePath = SolutionDirectory.Combine("files");
            server = new TestWebServer.HttpServer(filesToServePath, 50000);
            Task.Factory.StartNew(server.Start);
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }
    }
}

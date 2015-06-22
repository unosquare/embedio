namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Net;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class FluentTest
    {
        protected string RootPath;

        [SetUp]
        public void Init()
        {
            RootPath = TestHelper.SetupStaticFolder();
        }

        [Test]
        public void FluentWithStaticFolder()
        {
            var webServer = WebServer.Create(Resources.ServerAddress)
                .WithLocalSession()
                .WithStaticFolderAt(RootPath);

            Assert.AreEqual(webServer.Modules.Count, 2, "It has 2 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");
            Assert.AreEqual(webServer.Module<StaticFilesModule>().FileSystemPath, RootPath, "StaticFilesModule root path is equal to RootPath");

            webServer.RunAsync();

            var request = (HttpWebRequest)WebRequest.Create(Resources.ServerAddress);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                Assert.AreEqual(response.StatusCode, HttpStatusCode.OK, "Status Code OK");
            }

            webServer.Dispose();
        }

        [Test]
        public void FluentWithWebApi()
        {
            var webServer = WebServer.Create(Resources.ServerAddress)
                .WithWebApi(typeof(FluentTest).Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebApiModule>(), "It has WebApiModule");
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 2, "It has two controllers");

            webServer.Dispose();
        }

        [Test]
        public void FluentWithWebSockets()
        {
            var webServer = WebServer.Create(Resources.ServerAddress)
                .WithWebSocket(typeof(FluentTest).Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebSocketsModule>(), "It has WebSocketsModule");

            webServer.Dispose();
        }

        [Test]
        public void FluentLoadWebApiControllers()
        {
            var webServer = WebServer.Create(Resources.ServerAddress)
                .WithWebApi();
            webServer.Module<WebApiModule>().LoadApiControllers(typeof (FluentTest).Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebApiModule>(), "It has WebApiModule");
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 2, "It has two controllers");

            webServer.Dispose();
        }
    }
}
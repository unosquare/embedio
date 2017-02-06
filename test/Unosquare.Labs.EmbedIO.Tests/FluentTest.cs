namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Reflection;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class FluentTest
    {
        protected string RootPath;
        protected string WebServerUrl;

        [SetUp]
        public void Init()
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            WebServerUrl = Resources.GetServerAddress();
            RootPath = TestHelper.SetupStaticFolder();
        }
        
        [Test]
        public void FluentWithStaticFolder()
        {
            var webServer = WebServer.Create(WebServerUrl)
                .WithLocalSession()
                .WithStaticFolderAt(RootPath);

            Assert.AreEqual(webServer.Modules.Count, 2, "It has 2 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");
            Assert.AreEqual(webServer.Module<StaticFilesModule>().FileSystemPath, RootPath, "StaticFilesModule root path is equal to RootPath");
        }

        [Test]
        public void FluentWithWebApi()
        {
            var webServer = WebServer.Create(WebServerUrl)
                .WithWebApi(typeof(FluentTest).GetTypeInfo().Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebApiModule>(), "It has WebApiModule");
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 3, "It has three controllers");

            webServer.Dispose();
        }

        [Test]
        public void FluentWithWebSockets()
        {
            var webServer = WebServer.Create(WebServerUrl)
                .WithWebSocket(typeof(FluentTest).GetTypeInfo().Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebSocketsModule>(), "It has WebSocketsModule");

            webServer.Dispose();
        }

        [Test]
        public void FluentLoadWebApiControllers()
        {
            var webServer = WebServer.Create(WebServerUrl)
                .WithWebApi();
            webServer.Module<WebApiModule>().LoadApiControllers(typeof(FluentTest).GetTypeInfo().Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebApiModule>(), "It has WebApiModule");
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 3, "It has three controllers");

            webServer.Dispose();
        }
    }
}
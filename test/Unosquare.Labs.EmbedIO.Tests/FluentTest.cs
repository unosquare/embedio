namespace Unosquare.Labs.EmbedIO.Tests
{
    using Modules;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using TestObjects;

    [TestFixture]
    public class FluentTest
    {
        protected string RootPath;
        protected string WebServerUrl;
        protected WebServer NullWebServer = null;

        protected Dictionary<string, string> CommonPaths = new Dictionary<string, string>
        {
            {"/Server/web", TestHelper.SetupStaticFolder()},
            {"/Server/api", TestHelper.SetupStaticFolder()},
            {"/Server/database", TestHelper.SetupStaticFolder()}
        };

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
            Assert.AreEqual(webServer.Module<StaticFilesModule>().FileSystemPath, RootPath,
                "StaticFilesModule root path is equal to RootPath");
        }

        [Test]
        public void FluentWithWebApi()
        {
            var webServer = WebServer.Create(WebServerUrl)
                .WithWebApi(typeof(FluentTest).GetTypeInfo().Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebApiModule>(), "It has WebApiModule");
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 4, "It has four controllers");

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
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 4, "It has four controllers");

            webServer.Dispose();
        }

        [Test]
        public void FluentWithStaticFolderArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                NullWebServer.WithStaticFolderAt(TestHelper.SetupStaticFolder()));
        }

        [Test]
        public void FluentWithVirtualPaths()
        {
            var webServer = WebServer.Create(WebServerUrl)
                .WithVirtualPaths(CommonPaths);

            Assert.IsNotNull(webServer);
            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");
            Assert.AreEqual(webServer.Module<StaticFilesModule>().VirtualPaths.Count, 3, "It has 3 Virtual Paths");
        }

        [Test]
        public void FluentWithVirtualPathsWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                NullWebServer.WithVirtualPaths(CommonPaths));
        }

        [Test]
        public void FluentWithLocalSessionWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.WithLocalSession());
        }

        [Test]
        public void FluentWithWebApiArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.WithWebApi());
        }

        [Test]
        public void FluentWithWebSocketArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.WithWebSocket());
        }

        [Test]
        public void FluentLoadApiControllersWebServerArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.LoadApiControllers());
        }

        [Test]
        public void FluentLoadApiControllersWebApiModuleArgumentException()
        {
            WebApiModule webApi = null;

            Assert.Throws<ArgumentNullException>(() => webApi.LoadApiControllers());
        }

        [Test]
        public void FluentLoadWebSocketsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.LoadWebSockets());
        }

        [Test]
        public void FluentEnableCorsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.EnableCors());
        }

        [Test]
        public void FluentWithWebApiControllerTArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => NullWebServer.WithWebApiController<TestController>());
        }
    }
}
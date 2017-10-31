namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Reflection;
    using Modules;
    using TestObjects;
    using System;
    using System.Collections.Generic;

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
            Assert.Throws<ArgumentNullException>(() => {
                Extensions.WithStaticFolderAt(null, TestHelper.SetupStaticFolder());
            });                     
        }

        [Test]
        public void FluentWithVirtualPaths()
        {
            var paths = new Dictionary<string, string>
            {
                {"/Server/web", TestHelper.SetupStaticFolder()},
                {"/Server/api", TestHelper.SetupStaticFolder()},
                {"/Server/database", TestHelper.SetupStaticFolder()}
            };

            var webServer = WebServer.Create(WebServerUrl)
                .WithVirtualPaths(paths);

            Assert.IsNotNull(webServer);
            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");
            Assert.AreEqual(webServer.Module<StaticFilesModule>().VirtualPaths.Count, 3, "It has 3 Virtual Paths");
        }

        [Test]
        public void FluentWithVirtualPathsWebServerNull_ThrowsArgumentException()
        {
            var paths = new Dictionary<string, string>
            {
                {"/Server/web", TestHelper.SetupStaticFolder()},
                {"/Server/api", TestHelper.SetupStaticFolder()},
                {"/Server/database", TestHelper.SetupStaticFolder()}
            };

            Assert.Throws<ArgumentNullException>(() => {
                Extensions.WithVirtualPaths(null, paths);
            });
        }

        [Test]
        public void FluentWithLocalSessionWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => {
                Extensions.WithLocalSession(null);
            });
        }

        [Test]
        public void FluentWithWebApiArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => {
                Extensions.WithWebApi(null);
            });
        }

        [Test]
        public void FluentWithWebSocketArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => {
                Extensions.WithWebSocket(null);
            });
        }

        [Test]
        public void FluentLoadApiControllersWebServerArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentNullException>(() => {
                Extensions.LoadApiControllers(webServer);
            });
        }

        [Test]
        public void FluentLoadApiControllersWebApiModuleArgumentException()
        {
            WebApiModule webApi = null;

            Assert.Throws<ArgumentNullException>(() => {
                Extensions.LoadApiControllers(webApi);
            });
        }

        [Test]
        public void FluentLoadWebSocketsArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentNullException>(() => {
                Extensions.LoadWebSockets(webServer);
            });
        }

        [Test]
        public void FluentEnableCorsArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentNullException>(() => {
                Extensions.EnableCors(webServer);
            });
        }
        [Test]
        public void FluentWithWebApiControllerTArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentNullException>(() => {
                Extensions.WithWebApiController<TestController>(webServer);
            });
        }
    }
}
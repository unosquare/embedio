namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using System.Reflection;
    using Modules;
    using TestObjects;
    using System;
    using System.Collections.Generic;
    using System.IO;

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
            Assert.Throws<ArgumentException>(() => {
                Extensions.WithStaticFolderAt(null, TestHelper.SetupStaticFolder());
            });                     
        }

        [Test]
        public void FluentWithVirtualPaths()
        {
            Dictionary<string, string> paths = new Dictionary<string, string>();
            paths.Add("/Server/web", TestHelper.SetupStaticFolder());
            paths.Add("/Server/api", TestHelper.SetupStaticFolder());
            paths.Add("/Server/database", TestHelper.SetupStaticFolder());

            var webServer = WebServer.Create(WebServerUrl)
                .WithVirtualPaths(paths);

            Assert.IsNotNull(webServer);
            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");
            Assert.AreEqual(webServer.Module<StaticFilesModule>().VirtualPaths.Count, 3, "It has 3 Virtual Paths");
        }

        [Test]
        public void FluentWithVirtualPathsArgumentException()
        {
            Dictionary<string, string> paths = new Dictionary<string, string>();
            paths.Add("/Server/web", TestHelper.SetupStaticFolder());
            paths.Add("/Server/api", TestHelper.SetupStaticFolder());
            paths.Add("/Server/database", TestHelper.SetupStaticFolder());

            Assert.Throws<ArgumentException>(() => {
                Extensions.WithVirtualPaths(null, paths);
            });
        }

        [Test]
        public void FluentWithLocalSessionArgumentException()
        {
            Assert.Throws<ArgumentException>(() => {
                Extensions.WithLocalSession(null);
            });
        }

        [Test]
        public void FluentWithWebApiArgumentException()
        {
            Assert.Throws<ArgumentException>(() => {
                Extensions.WithWebApi(null);
            });
        }

        [Test]
        public void FluentWithWebSocketArgumentException()
        {
            Assert.Throws<ArgumentException>(() => {
                Extensions.WithWebSocket(null);
            });
        }

        [Test]
        public void FluentLoadApiControllersWebServerArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentException>(() => {
                Extensions.LoadApiControllers(webServer);
            });
        }

        [Test]
        public void FluentLoadApiControllersWebApiModuleArgumentException()
        {
            WebApiModule webApi = null;

            Assert.Throws<ArgumentException>(() => {
                Extensions.LoadApiControllers(webApi);
            });
        }

        [Test]
        public void FluentLoadWebSocketsArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentException>(() => {
                Extensions.LoadWebSockets(webServer);
            });
        }

        [Test]
        public void FluentEnableCorsArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentException>(() => {
                Extensions.EnableCors(webServer);
            });
        }
        [Test]
        public void FluentWithWebApiControllerTArgumentException()
        {
            WebServer webServer = null;

            Assert.Throws<ArgumentException>(() => {
                Extensions.WithWebApiController<TestController>(webServer);
            });
        }
    }
}
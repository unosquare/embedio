using System;
using System.Collections.Generic;
using System.Linq;
using EmbedIO.Modules;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class FluentTest
    {
        private readonly WebServer _nullWebServer = null;
        private readonly Dictionary<string, string> _commonPaths = new Dictionary<string, string>
        {
            {"/Server/web", TestHelper.SetupStaticFolder()},
            {"/Server/api", TestHelper.SetupStaticFolder()},
            {"/Server/database", TestHelper.SetupStaticFolder()},
        };

        private string _rootPath;
        private string _webServerUrl;

        [SetUp]
        public void Init()
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;

            _webServerUrl = Resources.GetServerAddress();
            _rootPath = TestHelper.SetupStaticFolder();
        }

        [Test]
        public void FluentWithStaticFolder()
        {
            var webServer = new WebServer(_webServerUrl)
                .WithLocalSessionManager()
                .WithStaticFolderAt("/", _rootPath);

            Assert.AreEqual(webServer.Modules.Count, 2, "It has 2 modules loaded");
            Assert.IsNotNull(webServer.Modules.OfType<StaticFilesModule>().FirstOrDefault(), "It has StaticFilesModule");

            Assert.AreEqual(
                webServer.Modules.FirstOrDefaultOfType<StaticFilesModule>().FileSystemPath,
                _rootPath,
                "StaticFilesModule root path is equal to RootPath");
        }

        [Test]
        public void FluentWithWebApi()
        {
            var webServer = new WebServer(_webServerUrl)
                .WithWebApi(typeof(FluentTest).Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Modules.OfType<WebApiModule>().FirstOrDefault(), "It has WebApiModule");
            Assert.AreEqual(webServer.Modules.OfType<WebApiModule>().First().ControllerCount, 4, "It has four controllers");

            webServer.Dispose();
        }

        [Test]
        public void FluentWithWebSockets()
        {
            var webServer = new WebServer(_webServerUrl)
                .WithWebSocket(typeof(FluentTest).Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebSocketModule>(), "It has WebSocketModule");

            (webServer as IDisposable)?.Dispose();
        }

        [Test]
        public void FluentLoadWebApiControllers()
        {
            var webServer = new WebServer(_webServerUrl)
                .WithWebApi();

            var webApiModule = webServer.Modules.OfType<WebApiModule>().First();
            Assert.IsNotNull(webApiModule);

            webServer.Modules.OfType<WebApiModule>().First().LoadApiControllers(typeof(FluentTest).Assembly);

            Assert.AreEqual(webApiModule.ControllerCount, 4, "It has four controllers");

            webServer.Dispose();
        }

        [Test]
        public void FluentWithStaticFolderArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _nullWebServer.WithStaticFolderAt("/", TestHelper.SetupStaticFolder()));
        }

        [Test]
        public void FluentWithLocalSessionManagerWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithLocalSessionManager());
        }

        [Test]
        public void FluentWithWebApiArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithWebApi());
        }

        [Test]
        public void FluentWithWebSocketArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithWebSocket());
        }

        [Test]
        public void FluentLoadApiControllersWebServerArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.LoadApiControllers());
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
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.LoadWebSockets());
        }

        [Test]
        public void FluentWithCorsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithCors());
        }

        [Test]
        public void FluentWithWebApiControllerTArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithWebApiController<TestController>());
        }
    }
}
﻿namespace Unosquare.Labs.EmbedIO.Tests
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
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            _webServerUrl = Resources.GetServerAddress();
            _rootPath = TestHelper.SetupStaticFolder();
        }

        [Test]
        public void FluentWithStaticFolder()
        {
            var webServer = WebServer.Create(_webServerUrl)
                .WithLocalSession()
                .WithStaticFolderAt(_rootPath);

            Assert.AreEqual(webServer.Modules.Count, 2, "It has 2 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");

            Assert.AreEqual(
                webServer.Module<StaticFilesModule>().FileSystemPath,
                _rootPath,
                "StaticFilesModule root path is equal to RootPath");
        }

        [Test]
        public void FluentWithWebApi()
        {
            var webServer = WebServer.Create(_webServerUrl)
                .WithWebApi(typeof(FluentTest).GetTypeInfo().Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebApiModule>(), "It has WebApiModule");
            Assert.AreEqual(webServer.Module<WebApiModule>().ControllersCount, 4, "It has four controllers");

            webServer.Dispose();
        }

        [Test]
        public void FluentWithWebSockets()
        {
            var webServer = WebServer.Create(_webServerUrl)
                .WithWebSocket(typeof(FluentTest).GetTypeInfo().Assembly);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<WebSocketsModule>(), "It has WebSocketsModule");

            webServer.Dispose();
        }

        [Test]
        public void FluentLoadWebApiControllers()
        {
            var webServer = WebServer.Create(_webServerUrl)
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
                _nullWebServer.WithStaticFolderAt(TestHelper.SetupStaticFolder()));
        }

        [Test]
        public void FluentWithVirtualPaths()
        {
            var webServer = WebServer.Create(_webServerUrl)
                .WithVirtualPaths(_commonPaths);

            Assert.IsNotNull(webServer);
            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Module<StaticFilesModule>(), "It has StaticFilesModule");
            Assert.AreEqual(webServer.Module<StaticFilesModule>().VirtualPaths.Count, 3, "It has 3 Virtual Paths");
        }

        [Test]
        public void FluentWithVirtualPathsWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                _nullWebServer.WithVirtualPaths(_commonPaths));
        }

        [Test]
        public void FluentWithLocalSessionWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithLocalSession());
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
        public void FluentEnableCorsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.EnableCors());
        }

        [Test]
        public void FluentWithWebApiControllerTArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithWebApiController<TestController>());
        }
    }
}
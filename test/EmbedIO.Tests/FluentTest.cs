using EmbedIO.Files;
using EmbedIO.Tests.TestObjects;
using EmbedIO.Utilities;
using NUnit.Framework;
using System;
using System.Linq;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class FluentTest
    {
        private readonly WebServer _nullWebServer = null;
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

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Modules.OfType<StaticFilesModule>().FirstOrDefault(), "It has StaticFilesModule");

            Assert.AreEqual(
                webServer.Modules.FirstOrDefaultOfType<StaticFilesModule>().FileSystemPath,
                _rootPath,
                "StaticFilesModule root path is equal to RootPath");
        }

        [Test]
        public void FluentWithStaticFolderArgumentException()
        {
            Assert.Throws<NullReferenceException>(() =>
                _nullWebServer.WithStaticFolderAt("/", TestHelper.SetupStaticFolder()));
        }

        [Test]
        public void FluentWithLocalSessionManagerWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<NullReferenceException>(() => _nullWebServer.WithLocalSessionManager());
        }

        [Test]
        public void FluentWithWebApiArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithWebApi("/", null));
        }
        
        [Test]
        public void FluentWithCorsArgumentException()
        {
            Assert.Throws<NullReferenceException>(() => _nullWebServer.WithCors());
        }
    }
}
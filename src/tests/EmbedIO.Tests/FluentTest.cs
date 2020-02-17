using System;
using EmbedIO.Files;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using System.Linq;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class FluentTest
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference - This is intentional for test purposes.
        private readonly WebServer _nullWebServer = null;
#pragma warning restore CS8625
        private string _rootPath = string.Empty;
        private string _webServerUrl = string.Empty;

        [SetUp]
        public void Init()
        {
            // Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;

            _webServerUrl = Resources.GetServerAddress();
            _rootPath = StaticFolder.RootPathOf(nameof(FluentTest));
        }

        [Test]
        public void FluentWithStaticFolder()
        {
            var webServer = new WebServer(_webServerUrl)
                .WithLocalSessionManager()
                .WithStaticFolder("/", _rootPath, true);

            Assert.AreEqual(webServer.Modules.Count, 1, "It has 1 modules loaded");
            Assert.IsNotNull(webServer.Modules.OfType<FileModule>().FirstOrDefault(), $"It has {nameof(FileModule)}");
        }

        [Test]
        public void FluentWithStaticFolderArgumentException()
        {
            Assert.Throws<NullReferenceException>(() =>
                _nullWebServer.WithStaticFolder("/", StaticFolder.RootPathOf(nameof(FluentWithStaticFolderArgumentException)), true));
        }

        [Test]
        public void FluentWithLocalSessionManagerWebServerNull_ThrowsArgumentException()
        {
            Assert.Throws<NullReferenceException>(() => _nullWebServer.WithLocalSessionManager());
        }

        [Test]
        public void FluentWithWebApiArgumentException()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference - This is the whole point of this test.
            Assert.Throws<ArgumentNullException>(() => _nullWebServer.WithWebApi("/", null));
#pragma warning restore CS8625
        }
        
        [Test]
        public void FluentWithCorsArgumentException()
        {
            Assert.Throws<NullReferenceException>(() => _nullWebServer.WithCors());
        }
    }
}
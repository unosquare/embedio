using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Unosquare.Labs.EmbedIO.Tests.TestObjects;

namespace Unosquare.Labs.EmbedIO.Tests
{
    public abstract class FixtureBase
    {
        private readonly Action<WebServer> _builder;
        private WebServer _webServer;

        protected FixtureBase(Action<WebServer> builder)
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;
            _builder = builder;
        }

        public string WebServerUrl { get; private set; }

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            _webServer = new WebServer(WebServerUrl);

            _builder(_webServer);
            var runTask = _webServer.RunAsync();
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(500).Wait();
            _webServer?.Dispose();
        }
    }
}
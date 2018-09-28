﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestObjects;

    public abstract class FixtureBase
    {
        private readonly Action<IWebServer> _builder;
        private readonly bool _useTestWebServer;
        private readonly RoutingStrategy _routeStrategy;

        public IWebServer _webServer;

        protected FixtureBase(Action<IWebServer> builder, RoutingStrategy routeStrategy = RoutingStrategy.Regex, bool useTestWebServer = false)
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            _builder = builder;
            _routeStrategy = routeStrategy;
            _useTestWebServer = useTestWebServer;
        }

        public string WebServerUrl { get; private set; }

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            _webServer = _useTestWebServer
                ? (IWebServer) new TestWebServer(_routeStrategy)
                : new WebServer(WebServerUrl, _routeStrategy);

            _builder(_webServer);
            _webServer.RunAsync();
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(500).Wait();
            _webServer?.Dispose();
        }

        public async Task<string> GetString(string partialUrl)
        {
            if (_webServer is TestWebServer testWebServer)
                return await testWebServer.GetClient().GetAsync(partialUrl);

            using (var client = new HttpClient())
            {
                var uri = new Uri(new Uri(WebServerUrl), partialUrl);
                return await client.GetStringAsync(uri);
            }
        }
    }
}
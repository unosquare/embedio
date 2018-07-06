namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestObjects;
    using Constants;

    public abstract class FixtureBase
    {
        private readonly Action<WebServer> _builder;
        public WebServer _webServer;
        private readonly RoutingStrategy _routeStrategy;

        protected FixtureBase(Action<WebServer> builder, RoutingStrategy routeSrtategy)
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;
            _builder = builder;
            _routeStrategy = routeSrtategy;
        }

        public string WebServerUrl { get; private set; }

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            _webServer = new WebServer(WebServerUrl, _routeStrategy);

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
            using (var client = new HttpClient())
            {
                //Determine the absolute Uri by combining with WebServerUrl
                Uri uri = new Uri(new Uri(WebServerUrl), partialUrl);
                return await client.GetStringAsync(uri);
            }
        }
    }
}
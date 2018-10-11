namespace Unosquare.Labs.EmbedIO.Tests
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

        protected FixtureBase(Action<IWebServer> builder, RoutingStrategy routeStrategy = RoutingStrategy.Regex, bool useTestWebServer = false)
        {
            Swan.Terminal.Settings.DisplayLoggingMessageType = Swan.LogMessageType.None;

            _builder = builder;
            _routeStrategy = routeStrategy;
            _useTestWebServer = useTestWebServer;
        }

        public string WebServerUrl { get; private set; }

        public IWebServer WebServerInstance { get; private set; }

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            WebServerInstance = _useTestWebServer
                ? (IWebServer)new TestWebServer(_routeStrategy)
                : new WebServer(WebServerUrl, _routeStrategy);

            _builder(WebServerInstance);
            WebServerInstance.RunAsync();
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(500).Wait();
            WebServerInstance?.Dispose();
        }

        public async Task<string> GetString(string partialUrl)
        {
            if (WebServerInstance is TestWebServer testWebServer)
                return await testWebServer.GetClient().GetAsync(partialUrl);

            using (var client = new HttpClient())
            {
                var uri = new Uri(new Uri(WebServerUrl), partialUrl);

                return await client.GetStringAsync(uri);
            }
        }

        public async Task<TestHttpResponse> SendAsync(TestHttpRequest request)
        {
            if (WebServerInstance is TestWebServer testWebServer)
                return await testWebServer.GetClient().SendAsync(request);

            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request.ToHttpRequestMessage());

                return response.ToTestHttpResponse();
            }
        }
    }

    internal static class TestExtensions
    {
        public static HttpRequestMessage ToHttpRequestMessage(this TestHttpRequest request)
        {
            return new HttpRequestMessage();
        }

        public static TestHttpResponse ToTestHttpResponse(this HttpResponseMessage response)
        {
            return new TestHttpResponse();
        }
    }
}
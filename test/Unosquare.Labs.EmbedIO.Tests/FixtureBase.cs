namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using NUnit.Framework;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using TestObjects;

    public abstract class FixtureBase : IDisposable
    {
        private readonly Action<IWebServer> _builder;
        private readonly bool _useTestWebServer;
        private readonly RoutingStrategy _routeStrategy;

        protected FixtureBase(Action<IWebServer> builder, RoutingStrategy routeStrategy = RoutingStrategy.Regex, bool useTestWebServer = false)
        {
            Swan.Terminal.Settings.GlobalLoggingMessageType = Swan.LogMessageType.None;

            _builder = builder;
            _routeStrategy = routeStrategy;
            _useTestWebServer = useTestWebServer;
        }

        ~FixtureBase()
        {
            Dispose(false);
        }

        public string WebServerUrl { get; private set; }

        public IWebServer WebServerInstance { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SetUp]
        public void Init()
        {
            WebServerUrl = Resources.GetServerAddress();
            WebServerInstance = _useTestWebServer
                ? (IWebServer)new TestWebServer(_routeStrategy)
                : new WebServer(WebServerUrl, _routeStrategy);

            _builder(WebServerInstance);
            OnAfterInit();
            WebServerInstance.RunAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            (WebServerInstance as IDisposable)?.Dispose();
        }

        protected virtual void OnAfterInit()
        {
        }

        [TearDown]
        public void Kill()
        {
            Task.Delay(500).Wait();
            (WebServerInstance as IDisposable)?.Dispose();
        }

        public async Task<string> GetString(string partialUrl = "")
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
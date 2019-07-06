using System;
using System.Net.Http;
using System.Threading.Tasks;
using EmbedIO.Testing;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using Unosquare.Swan;

namespace EmbedIO.Tests
{
    public abstract class EndToEndFixtureBase : IDisposable
    {
        private readonly Action<IWebServer> _builder;
        private readonly bool _useTestWebServer;

        protected EndToEndFixtureBase(Action<IWebServer> builder, bool useTestWebServer = false)
        {
            Terminal.Settings.GlobalLoggingMessageType = LogMessageType.None;

            _builder = builder;
            _useTestWebServer = useTestWebServer;
        }

        ~EndToEndFixtureBase()
        {
            Dispose(false);
        }

        protected string WebServerUrl { get; private set; }

        protected HttpClient Client { get; private set; }

        private IWebServer WebServerInstance { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [SetUp]
        public void SetUp()
        {
            WebServerUrl = Resources.GetServerAddress();
            if (_useTestWebServer)
            {
                var testWebServer = new TestWebServer(WebServerUrl);
                WebServerInstance = testWebServer;
                Client = testWebServer.Client;
            }
            else
            {
                WebServerInstance = new WebServer(WebServerUrl);
                Client = new HttpClient {
                    BaseAddress = new Uri(WebServerUrl),
                };
            }

            _builder(WebServerInstance);
            OnSetUp();
            WebServerInstance.RunAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Client?.Dispose();
            WebServerInstance?.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            Task.Delay(500).Await();
            WebServerInstance?.Dispose();
            OnTearDown();
        }

        protected virtual void OnSetUp()
        {
        }

        protected virtual void OnTearDown()
        {
        }
    }
}
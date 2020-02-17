using System;
using System.Threading.Tasks;
using EmbedIO.Testing;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using Swan;
using Swan.Logging;

namespace EmbedIO.Tests
{
    public abstract class EndToEndFixtureBase : IDisposable
    {
        private readonly bool _useTestWebServer;

#pragma warning disable CS8618 // Non-nullable property is uninitialized - Properties are initialized in SetUp method.
        protected EndToEndFixtureBase(bool useTestWebServer = true)
#pragma warning restore CS8618
        {
            _useTestWebServer = useTestWebServer;
        }

        ~EndToEndFixtureBase()
        {
            Dispose(false);
        }

        protected string WebServerUrl { get; private set; }

        protected TestHttpClient Client { get; private set; }

        protected IWebServer Server { get; set; }

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
                Server = testWebServer;
                Client = testWebServer.Client;
            }
            else
            {
                Server = new WebServer(WebServerUrl);
                Client = TestHttpClient.Create(WebServerUrl);
            }

            OnSetUp();
            Server.Start();
        }

        [TearDown]
        public void TearDown()
        {
            Task.Delay(500).Await();
            Server?.Dispose();
            OnTearDown();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Client?.Dispose();
            Server?.Dispose();
        }

        protected virtual void OnSetUp()
        {
        }

        protected virtual void OnTearDown()
        {
        }
    }

    [SetUpFixture]
    public class SetUpEndToEnd
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests() => Logger.NoLogging();
    }
}
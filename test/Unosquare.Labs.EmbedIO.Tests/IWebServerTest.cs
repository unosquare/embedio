namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using Mocks;
    using Modules;
    using Swan;

    public class IWebServerTest
    {
        [SetUp]
        public void Setup()
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
        }

        [Test]
        public void SetupInMemoryWebServer_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                Assert.IsNotNull(webserver);
            }
        }
        
        [Test]
        public void RegisterWebModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse("OK")));

                Assert.AreEqual(1, webserver.Modules.Count);
            }
        }

        [Test]
        public void UnregisterWebModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse("OK")));
                webserver.UnregisterModule(typeof(FallbackModule));

                Assert.AreEqual(0, webserver.Modules.Count);
            }
        }
        
        [Test]
        public void RegisterSessionModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new LocalSessionModule());

                Assert.NotNull(webserver.SessionModule);
            }
        }

        [Test]
        public void UnregisterSessionModule_ReturnsValidInstance()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new LocalSessionModule());
                webserver.UnregisterModule(typeof(LocalSessionModule));

                Assert.IsNull(webserver.SessionModule);
            }
        }

        [Test]
        public void RunsServerAndRequestDAta_ReturnsValidData()
        {
            using (var webserver = new TestWebServer())
            {
                webserver.RegisterModule(new FallbackModule((ctx, ct) => ctx.JsonResponse("OK")));
                webserver.RunAsync();
                var client = webserver.GetClient();

                client.GetResponse();

                Assert.IsNotNull(client.Context.Response);
            }
        }
    }
}

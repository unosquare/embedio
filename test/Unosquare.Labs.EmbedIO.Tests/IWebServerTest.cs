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
    }
}

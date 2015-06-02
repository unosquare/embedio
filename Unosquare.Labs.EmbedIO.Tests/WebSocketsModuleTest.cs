namespace Unosquare.Labs.EmbedIO.Tests
{
    using NUnit.Framework;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.Properties;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class WebSocketsModuleTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.ServerAddress, Logger).WithWebSocket(typeof(TestWebSocket).Assembly);
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebSocket()
        {
            Assert.IsNotNull(WebServer.Module<WebSocketsModule>(), "WebServer has WebSocketsModule");

            Assert.AreEqual(WebServer.Module<WebSocketsModule>().Handlers.Count, 1, "WebSocketModule has one handler");
        }
    }
}

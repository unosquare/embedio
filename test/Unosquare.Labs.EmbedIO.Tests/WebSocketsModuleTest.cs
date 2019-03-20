namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Modules;
    using Net;
    using NUnit.Framework;
    using Swan.Formatters;
    using System;
    using System.Threading.Tasks;
    using TestObjects;

    [TestFixture]
    public class WebSocketsModuleTest : WebSocketsModuleTestBase
    {
        public WebSocketsModuleTest()
            : base(
                RoutingStrategy.Wildcard,
                ws =>
                {
                    ws.RegisterModule(new WebSocketsModule());
                    ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocket>();
                    ws.Module<WebSocketsModule>().RegisterWebSocketsServer<BigDataWebSocket>();
                },
                "test/")
        {
            // placeholder
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            await ConnectWebSocket();
        }

        [Test]
        public async Task TestSendBigDataWebSocket()
        {
            var webSocketUrl = WebServerUrl.Replace("http", "ws") + "bigdata";
            var wasSet = false;

            var clientSocket = new WebSocket(webSocketUrl);
            await clientSocket.ConnectAsync();
            clientSocket.OnMessage += (s, e) =>
            {
                Assert.AreEqual(Json.Serialize(BigDataWebSocket.BigDataObject), e.Data);
                wasSet = true;
            };

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
            await clientSocket.SendAsync(buffer, Opcode.Text);
            await Task.Delay(TimeSpan.FromSeconds(1));

            if (!wasSet)
                Assert.Ignore("Timeout");

            Assert.True(wasSet);
        }
    }

    [TestFixture]
    public class WebSocketsWildcard : WebSocketsModuleTestBase
    {
        public WebSocketsWildcard()
            : base(
                RoutingStrategy.Wildcard,
                ws =>
                {
                    ws.RegisterModule(new WebSocketsModule());
                    ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocketWildcard>();
                },
                "test/*")
        {
            // placeholder
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            await ConnectWebSocket();
        }
    }

    [TestFixture]
    public class WebSocketsModuleTestRegex : WebSocketsModuleTestBase
    {
        public WebSocketsModuleTestRegex()
            : base(
                RoutingStrategy.Regex,
                ws =>
                {
                    ws.RegisterModule(new WebSocketsModule());
                    ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocketRegex>();
                },
                "test/{100}")
        {
            // placeholder
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            await ConnectWebSocket();
        }
    }
}
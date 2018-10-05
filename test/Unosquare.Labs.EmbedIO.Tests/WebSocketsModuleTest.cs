namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Swan.Formatters;
    using NUnit.Framework;
    using Modules;
    using TestObjects;
    using Constants;
#if NET47
    using System;
    using System.Net.WebSockets;
#else
    using Net;
#endif

    [TestFixture]
    public class WebSocketsModuleTest : WebSocketsModuleTestBase
    {
        public WebSocketsModuleTest()
            : base(RoutingStrategy.Wildcard, ws =>
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
            if (IgnoreWebConnect)
                Assert.Inconclusive("WebSocket Connect not available");

            var webSocketUrl = WebServerUrl.Replace("http", "ws") + "bigdata";

            var ct = new CancellationTokenSource();
#if NET47
            var clientSocket = new ClientWebSocket();
            await clientSocket.ConnectAsync(new Uri(webSocketUrl), ct.Token);

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
            var buffer = new ArraySegment<byte>(new byte[1024]);

            await clientSocket.SendAsync(message, WebSocketMessageType.Text, true, ct.Token);
            await clientSocket.ReceiveAsync(buffer, ct.Token);

            Assert.AreEqual(System.Text.Encoding.UTF8.GetString(buffer.Array).Substring(0, 100),
                Json.Serialize(BigDataWebSocket.BigDataObject).Substring(0, 100), 
                "Initial chars are equal");
#else
            var clientSocket = new WebSocket(webSocketUrl);
            await clientSocket.ConnectAsync(ct.Token);
            clientSocket.OnMessage += (s, e) =>
            {
                Assert.AreEqual(Json.Serialize(BigDataWebSocket.BigDataObject), e.Data);
            };

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
            await clientSocket.SendAsync(buffer, Opcode.Text, ct.Token);
            await Task.Delay(500, ct.Token);
#endif
        }
    }

    public class WebSocketsWildcard : WebSocketsModuleTestBase
    {
        public WebSocketsWildcard()
            : base(RoutingStrategy.Wildcard, ws =>
            {
                ws.RegisterModule(new WebSocketsModule());
                ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocketWildcard>();
            }, "test/*")
        {
            // placeholder
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            await ConnectWebSocket();
        }
    }

    public class WebSocketsModuleTestRegex : WebSocketsModuleTestBase
    {
        public WebSocketsModuleTestRegex()
            : base(RoutingStrategy.Regex, ws =>
            {
                ws.RegisterModule(new WebSocketsModule());
                ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocketRegex>();
            }, "test/{100}")
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
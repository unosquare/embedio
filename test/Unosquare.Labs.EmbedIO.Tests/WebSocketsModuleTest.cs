namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Swan.Formatters;
    using NUnit.Framework;
    using Modules;
    using TestObjects;
#if NET47
    using System.Net.WebSockets;
#else
    using Net;
#endif

    [TestFixture]
    public class WebSocketsModuleTest : FixtureBase
    {
        private readonly bool _ignoreWebConnect = Swan.Runtime.OS != Swan.OperatingSystem.Windows;

        public WebSocketsModuleTest() :
            base((ws) => 
            {
                ws.RegisterModule(new WebSocketsModule());
                ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocket>();
                ws.Module<WebSocketsModule>().RegisterWebSocketsServer<BigDataWebSocket>();
            }, Constants.RoutingStrategy.Wildcard)
        {
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            const string wsUrl = Resources.WsServerAddress + "test";
            Assert.IsNotNull(_webServer.Module<WebSocketsModule>(), "WebServer has WebSocketsModule");

            Assert.AreEqual(_webServer.Module<WebSocketsModule>().Handlers.Count, 1, "WebSocketModule has one handler");

            if (_ignoreWebConnect)
                Assert.Inconclusive("WebSocket Connect not available");

            var ct = new CancellationTokenSource();
#if NET47
            var clientSocket = new ClientWebSocket();
            await clientSocket.ConnectAsync(new Uri(wsUrl), ct.Token);

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
            var buffer = new ArraySegment<byte>(new byte[5]);

            await clientSocket.SendAsync(message, WebSocketMessageType.Text, true, ct.Token);
            await clientSocket.ReceiveAsync(buffer, ct.Token);
            Assert.AreEqual("HELLO", System.Text.Encoding.UTF8.GetString(buffer.Array).Trim(), "Final message is HELLO");
#else
            var clientSocket = new WebSocket(wsUrl);
            await clientSocket.ConnectAsync(ct.Token);
            clientSocket.OnMessage += (s, e) =>
            {
                Assert.AreEqual(e.Data, "HELLO");
            };
            
            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
            await clientSocket.SendAsync(buffer, Opcode.Text, ct.Token);
            await Task.Delay(500, ct.Token);
#endif
        }

        [Test]
        public async Task TestSendBigDataWebSocket()
        {
            if (_ignoreWebConnect)
                Assert.Inconclusive("WebSocket Connect not available");

            const string wsUrl = Resources.WsServerAddress + "bigdata";

            var ct = new CancellationTokenSource();
#if NET47
            var clientSocket = new ClientWebSocket();
            await clientSocket.ConnectAsync(new Uri(wsUrl), ct.Token);

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
            var buffer = new ArraySegment<byte>(new byte[1024]);

            await clientSocket.SendAsync(message, WebSocketMessageType.Text, true, ct.Token);
            await clientSocket.ReceiveAsync(buffer, ct.Token);
            
            Assert.AreEqual(System.Text.Encoding.UTF8.GetString(buffer.Array).Substring(0, 100), Json.Serialize(BigDataWebSocket.BigDataObject).Substring(0, 100), "Initial chars are equal");
#else
            var clientSocket = new WebSocket(wsUrl);
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
}

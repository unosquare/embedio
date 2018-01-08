namespace Unosquare.Labs.EmbedIO.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Swan.Formatters;
    using NUnit.Framework;
    using Modules;
    using TestObjects;
    using Unosquare.Labs.EmbedIO.Constants;
#if NET47
    using System;
    using System.Net.WebSockets;
#else
    using Net;
    using System;
#endif

    public class WebSocketsModuleTestBase : FixtureBase
    {
        private readonly string Url;
        protected readonly bool _ignoreWebConnect = Swan.Runtime.OS != Swan.OperatingSystem.Windows;

        public WebSocketsModuleTestBase(RoutingStrategy strategy, Action<WebServer> builder, string url) :
            base(builder ,strategy)
        {
            Url = url;
        }


        public virtual async Task TestConnectWebSocket()
        {

            var wsUrl = WebServerUrl.Replace("http", "ws") + Url;
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

    }
    [TestFixture]
    public class WebSocketsModuleTest : WebSocketsModuleTestBase
    {
        public WebSocketsModuleTest() : base(RoutingStrategy.Simple, (ws) =>
        {
            ws.RegisterModule(new WebSocketsModule());
            ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocket>();
            ws.Module<WebSocketsModule>().RegisterWebSocketsServer<BigDataWebSocket>();

        },"test/")
        {

        }

        [Test]
        public override async Task TestConnectWebSocket()
        {
            await base.TestConnectWebSocket();
        }

        [Test]
        public async Task TestSendBigDataWebSocket()
        {
            if (_ignoreWebConnect)
                Assert.Inconclusive("WebSocket Connect not available");

            var wsUrl = WebServerUrl.Replace("http", "ws") + "bigdata";

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

    public class WebSocketsWildcard : WebSocketsModuleTestBase
    {
        public WebSocketsWildcard() : base(RoutingStrategy.Wildcard, (ws) =>
        {
            ws.RegisterModule(new WebSocketsModule());
            ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocketWildcard>();

        }, "test/*")
        {

        }

        [Test]
        public override async Task TestConnectWebSocket()
        {
            await base.TestConnectWebSocket();
        }
    }

    public class WebSocketsModuleTestRegex : WebSocketsModuleTestBase
    {
        public WebSocketsModuleTestRegex() : base(RoutingStrategy.Regex, (ws) =>
        {
            ws.RegisterModule(new WebSocketsModule());
            ws.Module<WebSocketsModule>().RegisterWebSocketsServer<TestWebSocketRegex>();

        }, "test/{100}")
        {

        }

        [Test]
        public override async Task TestConnectWebSocket()
        {
            await base.TestConnectWebSocket();
        }
    }

}


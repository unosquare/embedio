namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Modules;
    using Constants;
#if NET47
    using System.Net.WebSockets;
#else
    using Net;
#endif

    public abstract class WebSocketsModuleTestBase : FixtureBase
    {
        private readonly string _url;

        protected WebSocketsModuleTestBase(RoutingStrategy strategy, Action<IWebServer> builder, string url)
            : base(builder, strategy)
        {
            _url = url;
        }

        protected bool IgnoreWebConnect => Swan.Runtime.OS != Swan.OperatingSystem.Windows;

        protected async Task ConnectWebSocket()
        {
            var wsUrl = WebServerUrl.Replace("http", "ws") + _url;
            Assert.IsNotNull(_webServer.Module<WebSocketsModule>(), "WebServer has WebSocketsModule");

            Assert.AreEqual(_webServer.Module<WebSocketsModule>().Handlers.Count, 1, "WebSocketModule has one handler");

            if (IgnoreWebConnect)
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
            clientSocket.OnMessage += (s, e) => { Assert.AreEqual(e.Data, "HELLO"); };

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
            await clientSocket.SendAsync(buffer, Opcode.Text, ct.Token);
            await Task.Delay(500, ct.Token);
#endif
        }
    }
}
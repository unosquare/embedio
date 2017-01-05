namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Reflection;
    using NUnit.Framework;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;
#if !NETCOREAPP1_1 && !NETSTANDARD1_6
    using System.Net.WebSockets;
#else
    using Unosquare.Net;
#endif

    [TestFixture]
    public class WebSocketsModuleTest
    {
        protected WebServer WebServer;

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.WsServerAddress.Replace("ws", "http")).WithWebSocket(typeof(TestWebSocket).GetTypeInfo().Assembly);
            WebServer.RunAsync();
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            var wsUrl = Resources.WsServerAddress + "test";
            Assert.IsNotNull(WebServer.Module<WebSocketsModule>(), "WebServer has WebSocketsModule");

            Assert.AreEqual(WebServer.Module<WebSocketsModule>().Handlers.Count, 1, "WebSocketModule has one handler");

#if !NETCOREAPP1_1 && !NETSTANDARD1_6
            var clientSocket = new ClientWebSocket();
            var ct = new CancellationTokenSource();
            await clientSocket.ConnectAsync(new Uri(wsUrl), ct.Token);

            Assert.AreEqual(clientSocket.State, WebSocketState.Open, "Connection is open");

            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
            var buffer = new ArraySegment<byte>(new byte[1024]);

            await clientSocket.SendAsync(message, WebSocketMessageType.Text, true, ct.Token);
            var result = await clientSocket.ReceiveAsync(buffer, ct.Token);

            Assert.IsTrue(result.EndOfMessage, "End of message is true");
            Assert.IsTrue(System.Text.Encoding.UTF8.GetString(buffer.Array).TrimEnd((char) 0) == "WELCOME", "Final message is WELCOME");
#else
            var clientSocket = new WebSocket(wsUrl);
            clientSocket.ConnectAsync();
            await Task.Delay(100);

            Assert.AreEqual(clientSocket.State, WebSocketState.Open, "Connection is open");

            clientSocket.Send("HOLA");
#endif
        }

        [TearDown]
        public void Kill()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            WebServer.Dispose();
        }
    }
}
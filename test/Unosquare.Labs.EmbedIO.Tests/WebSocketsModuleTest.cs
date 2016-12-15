namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
#if !NETCOREAPP1_1 && !NETSTANDARD1_6
    using System.Net.WebSockets;
#endif
    using System.Threading;
    using System.Threading.Tasks;
    using System.Reflection;
    using NUnit.Framework;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Labs.EmbedIO.Tests.TestObjects;

    [TestFixture]
    public class WebSocketsModuleTest
    {
        protected WebServer WebServer;
        protected TestConsoleLog Logger = new TestConsoleLog();

        [SetUp]
        public void Init()
        {
            WebServer = new WebServer(Resources.WsServerAddress.Replace("ws", "http"), Logger).WithWebSocket(typeof (TestWebSocket).GetTypeInfo().Assembly);
            WebServer.RunAsync();
        }

        [Test]
        public async Task TestConnectWebSocket()
        {
            Assert.IsNotNull(WebServer.Module<WebSocketsModule>(), "WebServer has WebSocketsModule");

            Assert.AreEqual(WebServer.Module<WebSocketsModule>().Handlers.Count, 1, "WebSocketModule has one handler");

#if !NETCOREAPP1_1 && !NETSTANDARD1_6
            var clientSocket = new ClientWebSocket();
            var ct = new CancellationTokenSource();
            await clientSocket.ConnectAsync(new Uri(Resources.WsServerAddress + "test"), ct.Token);

            Assert.AreEqual(clientSocket.State, WebSocketState.Open, "Connection is open");

            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
            var buffer = new ArraySegment<byte>(new byte[1024]);

            await clientSocket.SendAsync(message, WebSocketMessageType.Text, true, ct.Token);
            var result = await clientSocket.ReceiveAsync(buffer, ct.Token);

            Assert.IsTrue(result.EndOfMessage, "End of message is true");
            Assert.IsTrue(System.Text.Encoding.UTF8.GetString(buffer.Array).TrimEnd((char) 0) == "WELCOME", "Final message is WELCOME");
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
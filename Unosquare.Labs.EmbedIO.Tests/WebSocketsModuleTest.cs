namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Net.WebSockets;
    using System.Threading;
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
            WebServer = new WebServer(Resources.ServerAddress, Logger).WithWebSocket(typeof (TestWebSocket).Assembly);
            WebServer.RunAsync();
        }

        [Test]
        public void TestWebSocket()
        {
            Assert.IsNotNull(WebServer.Module<WebSocketsModule>(), "WebServer has WebSocketsModule");

            Assert.AreEqual(WebServer.Module<WebSocketsModule>().Handlers.Count, 1, "WebSocketModule has one handler");
        }

        [Test]
        public async void TestConnectWebSocket()
        {
            var clientSocket = new ClientWebSocket();
            var ct = new CancellationTokenSource();
            await clientSocket.ConnectAsync(new Uri(Resources.ServerAddress.Replace("http", "ws") + "/test"), ct.Token);

            Assert.AreEqual(clientSocket.State, WebSocketState.Open, "Connection is open");

            var message = new ArraySegment<byte>(System.Text.Encoding.Default.GetBytes("HOLA"));
            var buffer = new ArraySegment<byte>(new byte[1024]);

            await clientSocket.SendAsync(message, WebSocketMessageType.Text, true, ct.Token);
            var result = await clientSocket.ReceiveAsync(buffer, ct.Token);

            Assert.IsTrue(result.EndOfMessage);
            Assert.IsTrue(System.Text.Encoding.UTF8.GetString(buffer.Array).TrimEnd((char) 0) == "WELCOME");
        }
    }
}
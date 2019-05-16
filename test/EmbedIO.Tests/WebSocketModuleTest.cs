using System;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Modules;
using EmbedIO.Tests.TestObjects;
using NUnit.Framework;
using Unosquare.Swan.Formatters;

namespace EmbedIO.Tests
{
    [TestFixture]
    public class WebSocketModuleTest : WebSocketModuleTestBase
    {
        public WebSocketModuleTest()
            : base(
                RoutingStrategy.Wildcard,
                ws =>
                {
                    ws.RegisterModule(new WebSocketModule());
                    ws.Module<WebSocketModule>().RegisterWebSocketServer<TestWebSocket>();
                    ws.Module<WebSocketModule>().RegisterWebSocketServer<BigDataWebSocket>();
                    ws.Module<WebSocketModule>().RegisterWebSocketServer<CloseWebSocket>();
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
            var webSocketUrl = new Uri($"{WebServerUrl.Replace("http", "ws")}bigdata");

            var clientSocket = new System.Net.WebSockets.ClientWebSocket();
            await clientSocket.ConnectAsync(webSocketUrl, default);
            
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("HOLA"));
            await clientSocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, default);

            var json = await ReadString(clientSocket);
            Assert.AreEqual(Json.Serialize(BigDataWebSocket.BigDataObject), json);
        }

        [Test]
        public async Task TestWithDifferentCloseResponse()
        {
            var webSocketUrl = new Uri($"{WebServerUrl.Replace("http", "ws")}close");

            var clientSocket = new System.Net.WebSockets.ClientWebSocket();
            await clientSocket.ConnectAsync(webSocketUrl, default);

            var buffer = new ArraySegment<byte>(new byte[8192]);
            var result = await clientSocket.ReceiveAsync(buffer, default);

            Assert.IsTrue(result.CloseStatus.HasValue);
            Assert.IsTrue(result.CloseStatus.Value == System.Net.WebSockets.WebSocketCloseStatus.InvalidPayloadData);
        }
    }

    [TestFixture]
    public class WebSocketWildcard : WebSocketModuleTestBase
    {
        public WebSocketWildcard()
            : base(
                RoutingStrategy.Wildcard,
                ws =>
                {
                    ws.RegisterModule(new WebSocketModule());
                    ws.Module<WebSocketModule>().RegisterWebSocketsServer<TestWebSocketWildcard>();
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
    public class WebSocketModuleTestRegex : WebSocketModuleTestBase
    {
        public WebSocketModuleTestRegex()
            : base(
                RoutingStrategy.Regex,
                ws =>
                {
                    ws.RegisterModule(new WebSocketModule());
                    ws.Module<WebSocketModule>().RegisterWebSocketsServer<TestWebSocketRegex>();
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
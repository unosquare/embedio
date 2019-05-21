using System;
using System.Text;
using System.Threading.Tasks;
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
                WebApiRoutingStrategy.Wildcard,
                ws => ws
                    .WithModule(new TestWebSocket("/test"))
                    .WithModule(new BigDataWebSocket("/bigdata"))
                    .WithModule(new CloseWebSocket("/close")),
                "test/")
        {
        }

        [Test]
        public Task TestConnectWebSocket() => ConnectWebSocket();

        [Test]
        public async Task TestSendBigDataWebSocket()
        {
            var webSocketUrl = new Uri($"{WebServerUrl.Replace("http", "ws")}bigdata");

            var clientSocket = new System.Net.WebSockets.ClientWebSocket();
            await clientSocket.ConnectAsync(webSocketUrl, default).ConfigureAwait(false);
            
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("HOLA"));
            await clientSocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, default).ConfigureAwait(false);

            var json = await ReadString(clientSocket).ConfigureAwait(false);
            Assert.AreEqual(Json.Serialize(BigDataWebSocket.BigDataObject), json);
        }

        [Test]
        public async Task TestWithDifferentCloseResponse()
        {
            var webSocketUrl = new Uri($"{WebServerUrl.Replace("http", "ws")}close");

            var clientSocket = new System.Net.WebSockets.ClientWebSocket();
            await clientSocket.ConnectAsync(webSocketUrl, default).ConfigureAwait(false);

            var buffer = new ArraySegment<byte>(new byte[8192]);
            var result = await clientSocket.ReceiveAsync(buffer, default).ConfigureAwait(false);

            Assert.IsTrue(result.CloseStatus.HasValue);
            Assert.IsTrue(result.CloseStatus.Value == System.Net.WebSockets.WebSocketCloseStatus.InvalidPayloadData);
        }
    }
}
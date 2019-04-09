﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using System.Text;
    using Modules;
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
            var webSocketUrl = new Uri($"{WebServerUrl.Replace("http", "ws")}bigdata");

            var clientSocket = new System.Net.WebSockets.ClientWebSocket();
            await clientSocket.ConnectAsync(webSocketUrl, default);
            
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("HOLA"));
            await clientSocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, default);

            var json = await ReadString(clientSocket);
            Assert.AreEqual(Json.Serialize(BigDataWebSocket.BigDataObject), json);
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
namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Modules;
    using Constants;
    using Net;

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
            var wasSet = false;

            var clientSocket = new WebSocket(wsUrl);
            await clientSocket.ConnectAsync();
            clientSocket.OnMessage += (s, e) =>
            {
                Assert.AreEqual(e.Data, "HELLO");
                wasSet = true;
            };

            Assert.AreEqual(WebSocketState.Open, clientSocket.State, "Connection is open");

            var buffer = System.Text.Encoding.UTF8.GetBytes("HOLA");
            await clientSocket.SendAsync(buffer, Opcode.Text);
            await Task.Delay(500);

            Assert.IsTrue(wasSet);
        }
    }
}
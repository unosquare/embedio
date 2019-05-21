using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.Modules;
using NUnit.Framework;

namespace EmbedIO.Tests
{
    public abstract class WebSocketModuleTestBase : FixtureBase
    {
        private readonly string _url;

        protected WebSocketModuleTestBase(WebApiRoutingStrategy strategy, Action<IWebServer> builder, string url)
            : base(builder, strategy)
        {
            _url = url;
        }

        protected static async Task<string> ReadString(System.Net.WebSockets.ClientWebSocket ws)
        {
            var buffer = new ArraySegment<byte>(new byte[8192]);

            using (var ms = new MemoryStream())
            {
                System.Net.WebSockets.WebSocketReceiveResult result;

                do
                {
                    result = await ws.ReceiveAsync(buffer, default);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                }
                while (!result.EndOfMessage);

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
        
        protected async Task ConnectWebSocket()
        {
            var websocketUrl = new Uri(WebServerUrl.Replace("http", "ws") + _url);
            
            var clientSocket = new System.Net.WebSockets.ClientWebSocket();
            await clientSocket.ConnectAsync(websocketUrl, default);
            
            Assert.AreEqual(
                System.Net.WebSockets.WebSocketState.Open, 
                clientSocket.State, 
                $"Connection should be open, but the status is {clientSocket.State} - {websocketUrl}");

            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes("HOLA"));
            await clientSocket.SendAsync(buffer, System.Net.WebSockets.WebSocketMessageType.Text, true, default);

            Assert.AreEqual(await ReadString(clientSocket), "HELLO");
        }
    }
}
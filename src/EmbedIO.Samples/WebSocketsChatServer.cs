using System.Linq;
using EmbedIO.Modules;
using Unosquare.Swan;

namespace EmbedIO.Samples
{
    /// <inheritdoc />
    /// <summary>
    /// Defines a very simple chat server
    /// </summary>
    [WebSocketHandler("/chat")]
    public class WebSocketsChatServer : WebSocketsServer
    {
        public WebSocketsChatServer()
            : base(true)
        {
            // placeholder
        }
        
        /// <inheritdoc />
        protected override void OnMessageReceived(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            foreach (var ws in WebSockets.Where(ws => ws != context))
            {
                Send(ws, rxBuffer.ToText());
            }
        }

        
        /// <inheritdoc />
        public override string ServerName => nameof(WebSocketsChatServer);

        /// <inheritdoc />
        protected override void OnClientConnected(
            IWebSocketContext context, 
            System.Net.IPEndPoint localEndPoint,
            System.Net.IPEndPoint remoteEndPoint)
        {
            Send(context, "Welcome to the chat room!");

            foreach (var ws in WebSockets.Where(ws => ws != context))
            {
                Send(ws, "Someone joined the chat room.");
            }
        }
        
        /// <inheritdoc />
        protected override void OnFrameReceived(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            // placeholder
        }
        
        /// <inheritdoc />
        protected override void OnClientDisconnected(IWebSocketContext context)
        {
            Broadcast("Someone left the chat room.");
        }
    }
}
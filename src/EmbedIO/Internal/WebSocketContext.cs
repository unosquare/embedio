using System.Net.WebSockets;

namespace EmbedIO.Internal
{
    internal class WebSocketContext : IWebSocketContext
    {
        public WebSocketContext(IHttpContext httpContext, HttpListenerWebSocketContext webSocketContext)
        {
            HttpContext = httpContext;
            WebSocket = new WebSocket(webSocketContext.WebSocket);
        }

        /// <inheritdoc />
        public IHttpContext HttpContext { get; }

        /// <inheritdoc />
        public IWebSocket WebSocket { get; }
    }
}
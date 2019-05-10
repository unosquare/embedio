using System;

namespace EmbedIO
{
    /// <summary>
    /// Represents a wrapper around a regular WebSocketContext.
    /// </summary>
    public class WebSocketContext : IWebSocketContext
    {
        private readonly System.Net.WebSockets.HttpListenerWebSocketContext _webSocketContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketContext"/> class.
        /// </summary>
        /// <param name="webSocketContext">The web socket context.</param>
        public WebSocketContext(System.Net.WebSockets.HttpListenerWebSocketContext webSocketContext)
        {
            _webSocketContext = webSocketContext;
            WebSocket = new WebSocket(_webSocketContext.WebSocket);
            CookieCollection = new Internal.CookieCollection(_webSocketContext.CookieCollection);
        }

        /// <inheritdoc />
        public IWebSocket WebSocket { get; }

        /// <inheritdoc />
        public ICookieCollection CookieCollection { get; }

        /// <inheritdoc />
        public Uri RequestUri => _webSocketContext.RequestUri;
    }
}
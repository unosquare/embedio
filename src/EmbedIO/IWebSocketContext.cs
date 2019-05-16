namespace EmbedIO
{
    /// <summary>
    /// Interface to create a WebSocket Context.
    /// </summary>
    public interface IWebSocketContext
    {
        /// <summary>
        /// Gets the HTTP context of the WebSocket opening handshake.
        /// </summary>
        IHttpContext HttpContext { get; }

        /// <summary>
        /// Gets the web socket.
        /// </summary>
        /// <value>
        /// The web socket.
        /// </value>
        IWebSocket WebSocket { get; }
    }
}

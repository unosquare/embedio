namespace EmbedIO.WebSockets.Internal
{
    /// <summary>
    /// Represents a wrapper around a regular WebSocketContext.
    /// </summary>
    /// <inheritdoc />
    internal sealed class SystemWebSocketReceiveResult : IWebSocketReceiveResult
    {
        private readonly System.Net.WebSockets.WebSocketReceiveResult _results;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemWebSocketReceiveResult"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        public SystemWebSocketReceiveResult(System.Net.WebSockets.WebSocketReceiveResult results)
        {
            _results = results;
        }

        /// <inheritdoc/>
        public int Count => _results.Count;
        
        /// <inheritdoc/>
        public bool EndOfMessage=> _results.EndOfMessage;
        
        /// <inheritdoc/>
        public int MessageType => (int) _results.MessageType;
    }
}
#if !NETSTANDARD1_3
namespace Unosquare.Labs.EmbedIO
{
    /// <summary>
    /// Represents a wrapper around a regular WebSocketContext.
    /// </summary>
    /// <inheritdoc />
    public class WebSocketReceiveResult : IWebSocketReceiveResult
    {
        private readonly System.Net.WebSockets.WebSocketReceiveResult _results;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketReceiveResult"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        public WebSocketReceiveResult(System.Net.WebSockets.WebSocketReceiveResult results)
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
#endif

namespace Unosquare.Net
{
    using System;
    using System.Threading.Tasks;
    using Labs.EmbedIO;

    /// <summary>
    /// Provides access to the request and response objects used by the HttpListener class. This class cannot be inherited.
    /// </summary>
    public sealed class HttpListenerContext : IHttpContext
    {
        private WebSocketContext _websocketContext;

        internal HttpListenerContext(HttpConnection cnc)
        {
            Id = Guid.NewGuid();
            Connection = cnc;
            Request = new HttpListenerRequest(this);
            Response = new HttpListenerResponse(this);
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        internal HttpListenerRequest HttpListenerRequest => Request as HttpListenerRequest;

        internal HttpListenerResponse HttpListenerResponse => Response as HttpListenerResponse;
        
        internal HttpListener Listener { get; set; }

        internal int ErrorStatus { get; set; } = 400;

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection { get; }

        internal Guid Id { get; }

        /// <summary>
        /// Accepts a WebSocket handshake request.
        /// </summary>
        /// <returns>
        /// A <see cref="WebSocketContext" /> that represents
        /// the WebSocket handshake request.
        /// </returns>
        /// <exception cref="InvalidOperationException">This method has already been called.</exception>
        public async Task<WebSocketContext> AcceptWebSocketAsync()
        {
            if (_websocketContext != null)
                throw new InvalidOperationException("The accepting is already in progress.");

            _websocketContext = new WebSocketContext(this);
            await _websocketContext.WebSocket.InternalAcceptAsync();

            return _websocketContext;
        }
    }
}
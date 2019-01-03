namespace Unosquare.Net
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using Labs.EmbedIO;

    /// <summary>
    /// Provides access to the request and response objects used by the HttpListener class.
    /// This class cannot be inherited.
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
            User = null;
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }
        
        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        internal HttpListenerRequest HttpListenerRequest => Request as HttpListenerRequest;

        internal HttpListenerResponse HttpListenerResponse => Response as HttpListenerResponse;
        
        internal HttpListener Listener { get; set; }

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection { get; }

        internal Guid Id { get; }

        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize)
        {
            if (_websocketContext != null)
                throw new InvalidOperationException("The accepting is already in progress.");

            _websocketContext = new WebSocketContext(this);
            await ((WebSocket) _websocketContext.WebSocket).InternalAcceptAsync().ConfigureAwait(false);

            return _websocketContext;
        }
    }
}
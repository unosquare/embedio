using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using EmbedIO.Net.Internal;

namespace EmbedIO.Net
{
    /// <summary>
    /// Provides access to the request and response objects used by the HttpListener class.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class HttpListenerContext : IHttpContext
    {
        private WebSocketContext _websocketContext;
        private Lazy<IDictionary<object, object>> _items =
            new Lazy<IDictionary<object, object>>(() => new Dictionary<object, object>(), true);

        internal HttpListenerContext(HttpConnection cnc)
        {
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
        public string Id { get; }

        /// <inheritdoc />
        public IDictionary<object, object> Items
        {
            get => _items.Value;
            set => _items = new Lazy<IDictionary<object, object>>(() => value, true);
        }

        internal HttpListenerRequest HttpListenerRequest => Request as HttpListenerRequest;

        internal HttpListenerResponse HttpListenerResponse => Response as HttpListenerResponse;

        internal HttpListener Listener { get; set; }

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection { get; }


        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
        {
            if (_websocketContext != null)
                throw new InvalidOperationException("The accepting is already in progress.");

            _websocketContext = new WebSocketContext(this);
            await ((WebSocket)_websocketContext.WebSocket).InternalAcceptAsync().ConfigureAwait(false);

            return _websocketContext;
        }
    }
}
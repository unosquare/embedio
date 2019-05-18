using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using EmbedIO.Net.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;
using HttpListenerRequest = EmbedIO.Net.Internal.HttpListenerRequest;
using HttpListenerResponse = EmbedIO.Net.Internal.HttpListenerResponse;

namespace EmbedIO.Net
{
    /// <summary>
    /// Provides access to the request and response objects used by the HttpListener class.
    /// This class cannot be inherited.
    /// </summary>
    internal sealed class HttpListenerContext : IHttpContextImpl
    {
        private WebSocketContext _websocketContext;

        private readonly Lazy<IDictionary<object, object>> _items =
            new Lazy<IDictionary<object, object>>(() => new Dictionary<object, object>(), true);

        private readonly Stack<Action<IHttpContext>> _closeCallbacks;

        private bool _closed;

        internal HttpListenerContext(HttpConnection cnc)
        {
            Connection = cnc;
            Request = new HttpListenerRequest(this);
            Response = new HttpListenerResponse(this);
            User = null;
            Id = Request.RequestTraceIdentifier.ToString("D", CultureInfo.InvariantCulture);
            LocalEndPoint = Request.LocalEndPoint;
            RemoteEndPoint = Request.RemoteEndPoint;
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public ISessionProxy Session { get; set; }

        /// <inheritdoc />
        public IDictionary<object, object> Items => _items.Value;

        /// <inheritdoc />
        public void OnClose(Action<IHttpContext> callback)
        {
            if (_closed)
                throw new InvalidOperationException("HTTP context has already been closed.");

            _closeCallbacks.Push(Validate.NotNull(nameof(callback), callback));
        }

        internal HttpListenerRequest HttpListenerRequest => Request as HttpListenerRequest;

        internal HttpListenerResponse HttpListenerResponse => Response as HttpListenerResponse;

        internal HttpListener Listener { get; set; }

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection { get; }

        /// <inheritdoc />
        public void Close()
        {
            _closed = true;

            // Always close the response stream no matter what.
            Response.Close();

            foreach (var callback in _closeCallbacks)
            {
                try
                {
                    callback(this);
                }
                catch (Exception e)
                {
                    e.Log($"HTTP context", $"[Id] Exception thrown by a HTTP context close callback.");
                }
            }
        }

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
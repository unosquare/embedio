using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Net.Internal;
using EmbedIO.Sessions;
using EmbedIO.Utilities;
using EmbedIO.WebSockets;
using EmbedIO.WebSockets.Internal;
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
        private readonly Lazy<IDictionary<object, object>> _items =
            new Lazy<IDictionary<object, object>>(() => new Dictionary<object, object>(), true);

        private readonly Stack<Action<IHttpContext>> _closeCallbacks = new Stack<Action<IHttpContext>>();

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

        internal HttpListenerRequest HttpListenerRequest => Request as HttpListenerRequest;

        internal HttpListenerResponse HttpListenerResponse => Response as HttpListenerResponse;

        internal HttpListener Listener { get; set; }

        internal string ErrorMessage { get; set; }

        internal bool HaveError => ErrorMessage != null;

        internal HttpConnection Connection { get; }

        /// <inheritdoc />
        public void OnClose(Action<IHttpContext> callback)
        {
            if (_closed)
                throw new InvalidOperationException("HTTP context has already been closed.");

            _closeCallbacks.Push(Validate.NotNull(nameof(callback), callback));
        }

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
                    e.Log("HTTP context", $"[Id] Exception thrown by a HTTP context close callback.");
                }
            }
        }

        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(
            IEnumerable<string> requestedProtocols,
            string acceptedProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            CancellationToken cancellationToken)
        {
            var webSocket = await WebSocket.AcceptAsync(this, acceptedProtocol).ConfigureAwait(false);
            return new WebSocketContext(this, WebSocket.SupportedVersion, requestedProtocols, acceptedProtocol, webSocket, cancellationToken);
        }
    }
}
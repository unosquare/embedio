using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Security.Principal;
using System.Threading;
using EmbedIO.Sessions;
using EmbedIO.Utilities;

namespace EmbedIO.WebSockets.Internal
{
    internal sealed class WebSocketContext : IWebSocketContext
    {
        internal WebSocketContext(
            IHttpContextImpl httpContext,
            string webSocketVersion, 
            IEnumerable<string> requestedProtocols,
            string acceptedProtocol,
            IWebSocket webSocket,
            CancellationToken cancellationToken)
        {
            Id = UniqueIdGenerator.GetNext();
            CancellationToken = cancellationToken;
            HttpContextId = httpContext.Id;
            Session = httpContext.Session;
            Items = httpContext.Items;
            LocalEndPoint = httpContext.LocalEndPoint;
            RemoteEndPoint = httpContext.RemoteEndPoint;
            RequestUri = httpContext.Request.Url;
            Headers = httpContext.Request.Headers;
            Origin = Headers[HttpHeaderNames.Origin];
            RequestedProtocols = requestedProtocols;
            AcceptedProtocol = acceptedProtocol;
            WebSocketVersion = webSocketVersion;
            Cookies = httpContext.Request.Cookies;
            User = httpContext.User;
            IsAuthenticated = httpContext.User.Identity.IsAuthenticated;
            IsLocal = httpContext.Request.IsLocal;
            IsSecureConnection = httpContext.Request.IsSecureConnection;
            WebSocket = webSocket;
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; }

        /// <inheritdoc />
        public string HttpContextId { get; }

        /// <inheritdoc />
        public ISessionProxy Session { get; }

        /// <inheritdoc />
        public IDictionary<object, object> Items { get; }

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; }

        /// <inheritdoc />
        public Uri RequestUri { get; }

        /// <inheritdoc />
        public NameValueCollection Headers { get; }

        /// <inheritdoc />
        public string Origin { get; }

        /// <inheritdoc />
        public IEnumerable<string> RequestedProtocols { get; }

        /// <inheritdoc />
        public string AcceptedProtocol { get; }

        /// <inheritdoc />
        public string WebSocketVersion { get; }

        /// <inheritdoc />
        public ICookieCollection Cookies { get; }

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public bool IsAuthenticated { get; }

        /// <inheritdoc />
        public bool IsLocal { get; }

        /// <inheritdoc />
        public bool IsSecureConnection { get; }

        /// <inheritdoc />
        public IWebSocket WebSocket { get; }
    }
}
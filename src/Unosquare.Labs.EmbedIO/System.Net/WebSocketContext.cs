namespace Unosquare.Net
{
    using System;
    using Labs.EmbedIO;
    using System.Collections.Specialized;
    using System.IO;

    /// <summary>
    /// Provides the properties used to access the information in
    /// a WebSocket handshake request received by the <see cref="HttpListener" />.
    /// </summary>
    /// <seealso cref="IWebSocketContext" />
    internal class WebSocketContext
        : IWebSocketContext
    {
        private readonly HttpListenerContext _context;

        internal WebSocketContext(HttpListenerContext context)
        {
            _context = context;
            WebSocket = new WebSocket(this);
        }

        /// <inheritdoc />
        public ICookieCollection CookieCollection => _context.Request.Cookies;

        /// <summary>
        /// Gets the HTTP headers included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public NameValueCollection Headers => _context.Request.Headers;
        
        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is secured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secured; otherwise, <c>false</c>.
        /// </value>
        public bool IsSecureConnection => _context.Connection.IsSecure;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise, <c>false</c>.
        /// </value>
        public bool IsWebSocketRequest => _context.Request.IsWebSocketRequest;
        
        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the requested URI.
        /// </value>
        public Uri RequestUri => _context.Request.Url;
        
        /// <inheritdoc />
        public IWebSocket WebSocket { get; }

        internal Stream Stream => _context.Connection.Stream;

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() => _context.Request.ToString();

        internal void CloseAsync() => _context.Connection.Close(true);
    }
}
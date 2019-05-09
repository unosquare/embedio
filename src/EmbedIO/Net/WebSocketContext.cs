using System;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http.Headers;
using EmbedIO.Constants;

namespace EmbedIO.Net
{
    /// <summary>
    /// Provides the properties used to access the information in
    /// a WebSocket handshake request received by the <see cref="HttpListener" />.
    /// </summary>
    /// <seealso cref="IWebSocketContext" />
    public class WebSocketContext : IWebSocketContext
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
        /// Gets a value indicating whether the client connected from the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client connected from the local computer; otherwise, <c>false</c>.
        /// </value>
        public bool IsLocal => _context.Request.IsLocal;

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
        /// Gets the value of the Origin header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Origin header.
        /// </value>
        public string Origin => _context.Request.Headers[HttpRequestHeaders.];

        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the requested URI.
        /// </value>
        public Uri RequestUri => _context.Request.Url;

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in the request.
        /// </summary>
        /// <remarks>
        /// This property provides a part of the information used by the server to prove that
        /// it received a valid WebSocket handshake request.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Key header.
        /// </value>
        public string SecWebSocketKey => _context.Request.Headers[HttpRequestHeaders.WebSocketKey];

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the WebSocket protocol version.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Version header.
        /// </value>
        public string SecWebSocketVersion => _context.Request.Headers[HttpHeaders.WebSocketVersion];

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
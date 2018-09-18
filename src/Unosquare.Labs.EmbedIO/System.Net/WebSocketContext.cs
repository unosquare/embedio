namespace Unosquare.Net
{
    using System;
    using Labs.EmbedIO;
    using System.Collections.Specialized;
    using System.IO;

    /// <summary>
    /// Provides the properties used to access the information in
    /// a WebSocket handshake request received by the <see cref="HttpListener"/>.
    /// </summary>
    public class WebSocketContext
    {
        private readonly HttpListenerContext _context;

        internal WebSocketContext(HttpListenerContext context)
        {
            _context = context;
            WebSocket = new WebSocket(this);
        }

        /// <summary>
        /// Gets the HTTP cookies included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="CookieCollection"/> that contains the cookies.
        /// </value>
        public ICookieCollection CookieCollection => _context.Request.Cookies;

        /// <summary>
        /// Gets the HTTP headers included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public NameValueCollection Headers => _context.Request.Headers;

        /// <summary>
        /// Gets the value of the Host header included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Host header.
        /// </value>
        public string Host => _context.Request.Headers["Host"];

        /// <summary>
        /// Gets a value indicating whether the client connected from the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client connected from the local computer; otherwise, <c>false</c>.
        /// </value>
        public bool IsLocal => _context.Request.IsLocal;

#if SSL 
        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is secured.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secured; otherwise, <c>false</c>.
        /// </value>
        public bool IsSecureConnection => _context.Connection.IsSecure;
#endif

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
        public string Origin => _context.Request.Headers[Labs.EmbedIO.Constants.Headers.Origin];

        /// <summary>
        /// Gets the query string included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the query string parameters.
        /// </value>
        public NameValueCollection QueryString => _context.Request.QueryString;

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
        public string SecWebSocketKey => _context.Request.Headers[Labs.EmbedIO.Constants.Headers.WebSocketKey];

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the WebSocket protocol version.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Version header.
        /// </value>
        public string SecWebSocketVersion => _context.Request.Headers[Labs.EmbedIO.Constants.Headers.WebSocketVersion];

        /// <summary>
        /// Gets the server endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server endpoint.
        /// </value>
        public System.Net.IPEndPoint ServerEndPoint => _context.Connection.LocalEndPoint;

        /// <summary>
        /// Gets the client endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client endpoint.
        /// </value>
        public System.Net.IPEndPoint UserEndPoint => _context.Connection.RemoteEndPoint;

        /// <summary>
        /// Gets the <see cref="WebSocket"/> instance used for
        /// two-way communication between client and server.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocket"/>.
        /// </value>
        public WebSocket WebSocket { get; }

        internal Stream Stream => _context.Connection.Stream;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => _context.Request.ToString();

        internal void CloseAsync() => _context.Connection.Close(true);
    }
}
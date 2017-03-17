#if !NET46
#region License
/*
 * HttpListenerWebSocketContext.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2016 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Unosquare.Net
{
    /// <summary>
    /// Provides the properties used to access the information in
    /// a WebSocket handshake request received by the <see cref="HttpListener"/>.
    /// </summary>
    public class WebSocketContext
    {
        #region Private Fields

        private readonly HttpListenerContext _context;

        #endregion

        #region Internal Constructors
        
        internal WebSocketContext(HttpListenerContext context)
        {
            _context = context;
            WebSocket = new WebSocket(this);
        }

        #endregion

        #region Internal Properties
        
        internal Stream Stream => _context.Connection.Stream;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the HTTP cookies included in the request.
        /// </summary>
        /// <value>
        /// A <see cref="CookieCollection"/> that contains the cookies.
        /// </value>
        public CookieCollection CookieCollection => _context.Request.Cookies;

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
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public bool IsAuthenticated => _context.User != null;

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
        public string Origin => _context.Request.Headers["Origin"];

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
        public string SecWebSocketKey => _context.Request.Headers["Sec-WebSocket-Key"];
        
        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in the request.
        /// </summary>
        /// <remarks>
        /// This property represents the WebSocket protocol version.
        /// </remarks>
        /// <value>
        /// A <see cref="string"/> that represents the value of the Sec-WebSocket-Version header.
        /// </value>
        public string SecWebSocketVersion => _context.Request.Headers["Sec-WebSocket-Version"];

        /// <summary>
        /// Gets the server endpoint as an IP address and a port number.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server endpoint.
        /// </value>
        public System.Net.IPEndPoint ServerEndPoint => _context.Connection.LocalEndPoint;

        /// <summary>
        /// Gets the client information (identity, authentication, and security roles).
        /// </summary>
        /// <value>
        /// A <see cref="IPrincipal"/> instance that represents the client information.
        /// </value>
        public IPrincipal User => _context.User;

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

        #endregion

        #region Internal Methods

        internal async Task CloseAsync() => await _context.Connection.CloseAsync(true);

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() => _context.Request.ToString();

        #endregion
    }
}
#endif
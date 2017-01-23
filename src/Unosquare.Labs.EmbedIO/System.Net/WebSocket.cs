#if !NET46
#region License
/*
 * WebSocket.cs
 *
 * A C# implementation of the WebSocket interface.
 *
 * This code is derived from WebSocket.java
 * (http://github.com/adamac/Java-WebSocket-client).
 *
 * The MIT License
 *
 * Copyright (c) 2009 Adam MacBeth
 * Copyright (c) 2010-2016 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Frank Razenberg <frank@zzattack.org>
 * - David Wood <dpwood@gmail.com>
 * - Liryna <liryna.stark@gmail.com>
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
#if COMPAT
using Unosquare.Labs.EmbedIO.Log;
#else
using Unosquare.Swan;
#endif

namespace Unosquare.Net
{
    /// <summary>
    /// Indicates the state of a WebSocket connection.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://www.w3.org/TR/websockets/#dom-websocket-readystate">The WebSocket API</see>.
    /// </remarks>
    public enum WebSocketState : ushort
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates that the connection hasn't yet been established.
        /// </summary>
        Connecting = 0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates that the connection has been established,
        /// and the communication is possible.
        /// </summary>
        Open = 1,

        /// <summary>
        /// Equivalent to numeric value 2. Indicates that the connection is going through
        /// the closing handshake, or the <c>WebSocket.Close</c> method has been invoked.
        /// </summary>
        Closing = 2,

        /// <summary>
        /// Equivalent to numeric value 3. Indicates that the connection has been closed or
        /// couldn't be established.
        /// </summary>
        Closed = 3
    }

    /// <summary>
    /// Indicates the WebSocket frame type.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">
    /// Section 5.2</see> of RFC 6455.
    /// </remarks>
    internal enum Opcode : byte
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates continuation frame.
        /// </summary>
        Cont = 0x0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates text frame.
        /// </summary>
        Text = 0x1,

        /// <summary>
        /// Equivalent to numeric value 2. Indicates binary frame.
        /// </summary>
        Binary = 0x2,

        /// <summary>
        /// Equivalent to numeric value 8. Indicates connection close frame.
        /// </summary>
        Close = 0x8,

        /// <summary>
        /// Equivalent to numeric value 9. Indicates ping frame.
        /// </summary>
        Ping = 0x9,

        /// <summary>
        /// Equivalent to numeric value 10. Indicates pong frame.
        /// </summary>
        Pong = 0xa
    }

    /// <summary>
    /// Implements the WebSocket interface.
    /// </summary>
    /// <remarks>
    /// The WebSocket class provides a set of methods and properties for two-way communication using
    /// the WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
    /// </remarks>
    public class WebSocket : IDisposable
    {
        #region Private Fields

        private string _base64Key;
        private readonly bool _client;
        private Action _closeContext;
        private CompressionMethod _compression;
        private WebSocketContext _context;
        private bool _enableRedirection;
        private AutoResetEvent _exitReceiving;
        private string _extensions;
        private bool _extensionsRequested;
        private object _forMessageEventQueue;
        private object _forSend;
        private object _forState;
        private MemoryStream _fragmentsBuffer;
        private bool _fragmentsCompressed;
        private Opcode _fragmentsOpcode;
        private const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private bool _inContinuation;
        private volatile bool _inMessage;
        private Action<MessageEventArgs> _message;
        private Queue<MessageEventArgs> _messageEventQueue;
        private string _origin;
#if AUTHENTICATION
        private AuthenticationChallenge _authChallenge;
        private uint _nonceCount;
        private bool _preAuth;
        private NetworkCredential _proxyCredentials;
#endif
        private string _protocol;
        private readonly string[] _protocols;
        private bool _protocolsRequested;
#if PROXY
        private Uri _proxyUri;
#endif
        private volatile WebSocketState _readyState;
        private AutoResetEvent _receivePong;
#if SSL
        private ClientSslConfiguration _sslConfig;
#endif
        private Stream _stream;
        private TcpClient _tcpClient;
        private Uri _uri;
        private const string Version = "13";
        private TimeSpan _waitTime;

        #endregion

        #region Internal Fields

        /// <summary>
        /// Represents the empty array of <see cref="byte"/> used internally.
        /// </summary>
        internal static readonly byte[] EmptyBytes;

        /// <summary>
        /// Represents the length used to determine whether the data should be fragmented in sending.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The data will be fragmented if that length is greater than the value of this field.
        ///   </para>
        ///   <para>
        ///   If you would like to change the value, you must set it to a value between <c>125</c> and
        ///   <c>Int32.MaxValue - 14</c> inclusive.
        ///   </para>
        /// </remarks>
        internal static readonly int FragmentLength;

        /// <summary>
        /// Represents the random number generator used internally.
        /// </summary>
        internal static readonly RandomNumberGenerator RandomNumber;

        #endregion

        #region Static Constructor

        static WebSocket()
        {
            EmptyBytes = new byte[0];
            FragmentLength = 1016;
            RandomNumber = RandomNumberGenerator.Create();
        }

        #endregion

        #region Internal Constructors

        // As server
        internal WebSocket(WebSocketContext context, string protocol)
        {
            _context = context;
            _protocol = protocol;

            _closeContext = context.Close;
#if COMPAT
            Log = context.Log;
#endif
            _message = Messages;
            IsSecure = context.IsSecureConnection;
            _stream = context.Stream;
            _waitTime = TimeSpan.FromSeconds(1);

            Init();
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocket" /> class with
        /// the specified WebSocket URL and subprotocols.
        /// </summary>
        /// <param name="url">A <see cref="string" /> that represents the WebSocket URL to connect.</param>
        /// <param name="protocols">An array of <see cref="string" /> that contains the WebSocket subprotocols if any.
        /// Each value of <paramref name="protocols" /> must be a token defined in
        /// <see href="http://tools.ietf.org/html/rfc2616#section-2.2">RFC 2616</see>.</param>
        /// <exception cref="System.ArgumentNullException">url</exception>
        /// <exception cref="System.ArgumentException">
        /// An empty string. - url
        /// or
        /// url
        /// or
        /// protocols
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="url" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><para>
        ///   <paramref name="url" /> is invalid.
        /// </para>
        /// <para>
        /// -or-
        /// </para>
        /// <para>
        ///   <paramref name="protocols" /> is invalid.
        /// </para></exception>
#if COMPAT
        public WebSocket(string url, ILog logger, params string[] protocols)
#else
        public WebSocket(string url, params string[] protocols)
#endif
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (url.Length == 0)
                throw new ArgumentException("An empty string.", nameof(url));

            string msg;
            if (!url.TryCreateWebSocketUri(out _uri, out msg))
                throw new ArgumentException(msg, nameof(url));

            if (protocols != null && protocols.Length > 0)
            {
                msg = protocols.CheckIfValidProtocols();
                if (msg != null)
                    throw new ArgumentException(msg, nameof(protocols));

                _protocols = protocols;
            }

#if COMPAT
            Log = logger ?? new NullLog();
#endif

            _base64Key = CreateBase64Key();
            _client = true;

            _message = Messagec;
            IsSecure = _uri.Scheme == "wss";
            _waitTime = TimeSpan.FromSeconds(5);

            Init();
        }

        #endregion

        #region Internal Properties

        internal CookieCollection CookieCollection { get; private set; }

        // As server
        internal Func<WebSocketContext, string> CustomHandshakeRequestChecker { get; set; }

        internal bool HasMessage
        {
            get
            {
                lock (_forMessageEventQueue)
                    return _messageEventQueue.Count > 0;
            }
        }

        // As server
        internal bool IgnoreExtensions { get; set; } = true;

        internal bool IsConnected => _readyState == WebSocketState.Open || _readyState == WebSocketState.Closing;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the compression method used to compress a message on the WebSocket connection.
        /// </summary>
        /// <value>
        /// One of the <see cref="CompressionMethod"/> enum values, specifies the compression method
        /// used to compress a message. The default value is <see cref="CompressionMethod.None"/>.
        /// </value>
        public CompressionMethod Compression
        {
            get { return _compression; }

            set
            {
                lock (_forState)
                {
                    string msg;
                    if (!checkIfAvailable(true, false, true, false, false, true, out msg))
                    {
#if COMPAT
                        Log.Error(msg);
#else
                        msg.Error();
#endif
                        Error("An error has occurred in setting the compression.", null);

                        return;
                    }

                    _compression = value;
                }
            }
        }

        /// <summary>
        /// Gets the HTTP cookies included in the WebSocket handshake request and response.
        /// </summary>
        /// <value>
        /// An <see cref="T:System.Collections.Generic.IEnumerable{WebSocketSharp.Net.Cookie}"/>
        /// instance that provides an enumerator which supports the iteration over the collection of
        /// the cookies.
        /// </value>
        public IEnumerable<Cookie> Cookies
        {
            get
            {
                lock (CookieCollection.SyncRoot)
                    foreach (Cookie cookie in CookieCollection)
                        yield return cookie;
            }
        }

#if AUTHENTICATION
/// <summary>
/// Gets the credentials for the HTTP authentication (Basic/Digest).
/// </summary>
/// <value>
/// A <see cref="NetworkCredential"/> that represents the credentials for
/// the authentication. The default value is <see langword="null"/>.
/// </value>
        public NetworkCredential Credentials { get; }
#endif

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="WebSocket"/> emits
        /// a <see cref="OnMessage"/> event when receives a ping.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="WebSocket"/> emits a <see cref="OnMessage"/> event
        /// when receives a ping; otherwise, <c>false</c>. The default value is <c>false</c>.
        /// </value>
        public bool EmitOnPing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="WebSocket"/> redirects
        /// the handshake request to the new URL located in the handshake response.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="WebSocket"/> redirects the handshake request to
        /// the new URL; otherwise, <c>false</c>. The default value is <c>false</c>.
        /// </value>
        public bool EnableRedirection
        {
            get { return _enableRedirection; }

            set
            {
                lock (_forState)
                {
                    string msg;
                    if (!checkIfAvailable(true, false, true, false, false, true, out msg))
                    {
#if COMPAT
                        Log.Error(msg);
#else
                        msg.Error();
#endif
                        Error("An error has occurred in setting the enable redirection.", null);

                        return;
                    }

                    _enableRedirection = value;
                }
            }
        }

        /// <summary>
        /// Gets the WebSocket extensions selected by the server.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the extensions if any.
        /// The default value is <see cref="String.Empty"/>.
        /// </value>
        public string Extensions => _extensions ?? string.Empty;

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is alive.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is alive; otherwise, <c>false</c>.
        /// </value>
        public bool IsAlive => Ping();

        /// <summary>
        /// Gets a value indicating whether the WebSocket connection is secure.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        public bool IsSecure { get; private set; }

#if COMPAT
/// <summary>
/// Gets the logging functions.
/// </summary>
/// <value>
/// A <see cref="ILog"/> that provides the logging functions.
/// </value>
        public ILog Log { get; set; }
#endif

        /// <summary>
        /// Gets or sets the value of the HTTP Origin header to send with
        /// the WebSocket handshake request to the server.
        /// </summary>
        /// <remarks>
        /// The <see cref="WebSocket"/> sends the Origin header if this property has any.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of
        ///   the <see href="http://tools.ietf.org/html/rfc6454#section-7">Origin</see> header to send.
        ///   The default value is <see langword="null"/>.
        ///   </para>
        ///   <para>
        ///   The Origin header has the following syntax:
        ///   <c>&lt;scheme&gt;://&lt;host&gt;[:&lt;port&gt;]</c>
        ///   </para>
        /// </value>
        public string Origin
        {
            get { return _origin; }

            set
            {
                lock (_forState)
                {
                    string msg;
                    if (!checkIfAvailable(true, false, true, false, false, true, out msg))
                    {
#if COMPAT
                        Log.Error(msg);
#else
                        msg.Error();
#endif
                        Error("An error has occurred in setting the origin.", null);

                        return;
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        _origin = value;
                        return;
                    }

                    Uri origin;
                    if (!Uri.TryCreate(value, UriKind.Absolute, out origin) || origin.Segments.Length > 1)
                    {
#if COMPAT
                        Log.Error("The syntax of an origin must be '<scheme>://<host>[:<port>]'.");
#else
                        "The syntax of an origin must be '<scheme>://<host>[:<port>]'.".Error();
#endif
                        Error("An error has occurred in setting the origin.", null);

                        return;
                    }

                    _origin = value.TrimEnd('/');
                }
            }
        }

        /// <summary>
        /// Gets the WebSocket subprotocol selected by the server.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the subprotocol if any.
        /// The default value is <see cref="String.Empty"/>.
        /// </value>
        public string Protocol
        {
            get { return _protocol ?? string.Empty; }

            internal set { _protocol = value; }
        }

        /// <summary>
        /// Gets the state of the WebSocket connection.
        /// </summary>
        /// <value>
        /// One of the <see cref="WebSocketState"/> enum values, indicates the state of the connection.
        /// The default value is <see cref="WebSocketState.Connecting"/>.
        /// </value>
        public WebSocketState State => _readyState;

#if SSL
/// <summary>
/// Gets or sets the SSL configuration used to authenticate the server and
/// optionally the client for secure connection.
/// </summary>
/// <value>
/// A <see cref="ClientSslConfiguration"/> that represents the configuration used
/// to authenticate the server and optionally the client for secure connection,
/// or <see langword="null"/> if the <see cref="WebSocket"/> is used in a server.
/// </value>
        public ClientSslConfiguration SslConfiguration
        {
            get
            {
                return _client
                       ? (_sslConfig ?? (_sslConfig = new ClientSslConfiguration(_uri.DnsSafeHost)))
                       : null;
            }

            set
            {
                lock (_forState)
                {
                    string msg;
                    if (!checkIfAvailable(true, false, true, false, false, true, out msg))
                    {
                        Log.Error(msg);
                        error("An error has occurred in setting the ssl configuration.", null);

                        return;
                    }

                    _sslConfig = value;
                }
            }
        }
#endif

        /// <summary>
        /// Gets the WebSocket URL used to connect, or accepted.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the URL used to connect, or accepted.
        /// </value>
        public Uri Url => _client ? _uri : _context.RequestUri;

        /// <summary>
        /// Gets or sets the wait time for the response to the Ping or Close.
        /// </summary>
        /// <value>
        /// A <see cref="TimeSpan"/> that represents the wait time. The default value is the same as
        /// 5 seconds, or 1 second if the <see cref="WebSocket"/> is used in a server.
        /// </value>
        public TimeSpan WaitTime
        {
            get { return _waitTime; }

            set
            {
                lock (_forState)
                {
                    string msg;
                    if (!checkIfAvailable(true, true, true, false, false, true, out msg) ||
                        !value.CheckWaitTime(out msg))
                    {
#if COMPAT
                        Log.Error(msg);
#else
                        msg.Error();
#endif
                        Error("An error has occurred in setting the wait time.", null);

                        return;
                    }

                    _waitTime = value;
                }
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when the WebSocket connection has been closed.
        /// </summary>
        public event EventHandler<CloseEventArgs> OnClose;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> gets an error.
        /// </summary>
        public event EventHandler<ConnectionFailureEventArgs> OnError;

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> receives a message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

        /// <summary>
        /// Occurs when the WebSocket connection has been established.
        /// </summary>
        public event EventHandler OnOpen;

        #endregion

        #region Private Methods

        // As server
        private bool accept()
        {
            lock (_forState)
            {
                string msg;
                if (!checkIfAvailable(true, false, false, false, out msg))
                {
#if COMPAT
                        Log.Error(msg);
#else
                    msg.Error();
#endif
                    Error("An error has occurred in accepting.", null);

                    return false;
                }

                try
                {
                    if (!AcceptHandshake())
                        return false;

                    _readyState = WebSocketState.Open;
                }
                catch (Exception ex)
                {
#if COMPAT
                        Log.Error(ex);
#else
                    ex.Log(nameof(WebSocket));
#endif
                    Fatal("An exception has occurred while accepting.", ex);

                    return false;
                }

                return true;
            }
        }

        // As server
        private bool AcceptHandshake()
        {
#if COMPAT
            Log.DebugFormat("A request from {0}:\n{1}", _context.UserEndPoint, _context);
#else
            $"A request from {_context.UserEndPoint}:\n{_context}".Debug();
#endif

            string msg;
            if (!CheckHandshakeRequest(_context, out msg))
            {
                SendHttpResponse(CreateHandshakeFailureResponse(HttpStatusCode.BadRequest));

#if COMPAT
                        Log.Error(msg);
#else
                msg.Error();
#endif
                Fatal("An error has occurred while accepting.", CloseStatusCode.ProtocolError);

                return false;
            }

            if (!CustomCheckHandshakeRequest(_context, out msg))
            {
                SendHttpResponse(CreateHandshakeFailureResponse(HttpStatusCode.BadRequest));

#if COMPAT
                        Log.Error(msg);
#else
                msg.Error();
#endif
                Fatal("An error has occurred while accepting.", CloseStatusCode.PolicyViolation);

                return false;
            }

            _base64Key = _context.Headers["Sec-WebSocket-Key"];

            if (_protocol != null)
                ProcessSecWebSocketProtocolHeader(_context.SecWebSocketProtocols);

            if (!IgnoreExtensions)
                ProcessSecWebSocketExtensionsClientHeader(_context.Headers["Sec-WebSocket-Extensions"]);

            return SendHttpResponse(CreateHandshakeResponse());
        }

        // As server
        private bool CheckHandshakeRequest(WebSocketContext context, out string message)
        {
            message = null;

            if (context.RequestUri == null)
            {
                message = "Specifies an invalid Request-URI.";
                return false;
            }

            if (!context.IsWebSocketRequest)
            {
                message = "Not a WebSocket handshake request.";
                return false;
            }

            var headers = context.Headers;
            if (!ValidateSecWebSocketKeyHeader(headers["Sec-WebSocket-Key"]))
            {
                message = "Includes no Sec-WebSocket-Key header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketVersionClientHeader(headers["Sec-WebSocket-Version"]))
            {
                message = "Includes no Sec-WebSocket-Version header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketProtocolClientHeader(headers["Sec-WebSocket-Protocol"]))
            {
                message = "Includes an invalid Sec-WebSocket-Protocol header.";
                return false;
            }

            if (!IgnoreExtensions
                && !string.IsNullOrWhiteSpace(headers["Sec-WebSocket-Extensions"]))
            {
                message = "Includes an invalid Sec-WebSocket-Extensions header.";
                return false;
            }

            return true;
        }

        // As client
        private bool CheckHandshakeResponse(HttpResponse response, out string message)
        {
            message = null;

            if (response.IsRedirect)
            {
                message = "Indicates the redirection.";
                return false;
            }

            if (response.IsUnauthorized)
            {
                message = "Requires the authentication.";
                return false;
            }

            if (!response.IsWebSocketResponse)
            {
                message = "Not a WebSocket handshake response.";
                return false;
            }

            var headers = response.Headers;
            if (!ValidateSecWebSocketAcceptHeader(headers["Sec-WebSocket-Accept"]))
            {
                message = "Includes no Sec-WebSocket-Accept header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketProtocolServerHeader(headers["Sec-WebSocket-Protocol"]))
            {
                message = "Includes no Sec-WebSocket-Protocol header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketExtensionsServerHeader(headers["Sec-WebSocket-Extensions"]))
            {
                message = "Includes an invalid Sec-WebSocket-Extensions header.";
                return false;
            }

            if (!ValidateSecWebSocketVersionServerHeader(headers["Sec-WebSocket-Version"]))
            {
                message = "Includes an invalid Sec-WebSocket-Version header.";
                return false;
            }

            return true;
        }

        private bool checkIfAvailable(
            bool connecting, bool open, bool closing, bool closed, out string message
        )
        {
            message = null;

            if (!connecting && _readyState == WebSocketState.Connecting)
            {
                message = "This operation isn't available in: connecting";
                return false;
            }

            if (!open && _readyState == WebSocketState.Open)
            {
                message = "This operation isn't available in: open";
                return false;
            }

            if (!closing && _readyState == WebSocketState.Closing)
            {
                message = "This operation isn't available in: closing";
                return false;
            }

            if (!closed && _readyState == WebSocketState.Closed)
            {
                message = "This operation isn't available in: closed";
                return false;
            }

            return true;
        }

        private bool checkIfAvailable(
            bool client,
            bool server,
            bool connecting,
            bool open,
            bool closing,
            bool closed,
            out string message
        )
        {
            message = null;

            if (!client && _client)
            {
                message = "This operation isn't available in: client";
                return false;
            }

            if (!server && !_client)
            {
                message = "This operation isn't available in: server";
                return false;
            }

            return checkIfAvailable(connecting, open, closing, closed, out message);
        }

#if AUTHENTICATION
        private static bool CheckParametersForSetCredentials(
          string username, string password, out string message
        )
        {
            message = null;

            if (string.IsNullOrEmpty(username))
                return true;

            if (username.Contains(':') || !username.IsText())
            {
                message = "'username' contains an invalid character.";
                return false;
            }

            if (string.IsNullOrEmpty(password))
                return true;

            if (!password.IsText())
            {
                message = "'password' contains an invalid character.";
                return false;
            }

            return true;
        }
#endif
#if PROXY
        private static bool CheckParametersForSetProxy(
            string url, string username, string password, out string message
        )
        {
            message = null;

            if (string.IsNullOrEmpty(url))
                return true;

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)
                || uri.Scheme != "http"
                || uri.Segments.Length > 1
            )
            {
                message = "'url' is an invalid URL.";
                return false;
            }

            if (string.IsNullOrEmpty(username))
                return true;

            if (username.Contains(':') || !username.IsText())
            {
                message = "'username' contains an invalid character.";
                return false;
            }

            if (string.IsNullOrEmpty(password))
                return true;

            if (!password.IsText())
            {
                message = "'password' contains an invalid character.";
                return false;
            }

            return true;
        }
#endif

        private bool CheckReceivedFrame(WebSocketFrame frame, out string message)
        {
            message = null;

            var masked = frame.IsMasked;
            if (_client && masked)
            {
                message = "A frame from the server is masked.";
                return false;
            }

            if (!_client && !masked)
            {
                message = "A frame from a client isn't masked.";
                return false;
            }

            if (_inContinuation && frame.IsData)
            {
                message = "A data frame has been received while receiving continuation frames.";
                return false;
            }

            if (frame.IsCompressed && _compression == CompressionMethod.None)
            {
                message = "A compressed frame has been received without any agreement for it.";
                return false;
            }

            if (frame.Rsv2 == Rsv.On)
            {
                message = "The RSV2 of a frame is non-zero without any negotiation for it.";
                return false;
            }

            if (frame.Rsv3 == Rsv.On)
            {
                message = "The RSV3 of a frame is non-zero without any negotiation for it.";
                return false;
            }

            return true;
        }

        private void close(CloseEventArgs e, bool send, bool receive, bool received)
        {
            lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
#if COMPAT
                    Log.Info("The closing is already in progress.");
#else
                    "The closing is already in progress.".Info();
#endif
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
#if COMPAT
                    Log.Info("The connection has been closed.");
#else
                    "The connection has been closed.".Info();
#endif
                    return;
                }

                send = send && _readyState == WebSocketState.Open;
                receive = receive && send;

                _readyState = WebSocketState.Closing;
            }

#if COMPAT
            Log.Info("Begin closing the connection.");
#else
            "Begin closing the connection.".Info();
#endif

            var bytes = send ? WebSocketFrame.CreateCloseFrame(e.PayloadData, _client).ToArray() : null;
            e.WasClean = CloseHandshake(bytes, receive, received);
            ReleaseResources();

#if COMPAT
            Log.Info("End closing the connection.");
#else
            "End closing the connection.".Info();
#endif

            _readyState = WebSocketState.Closed;

            try
            {
                OnClose?.Invoke(this, e);
            }
            catch (Exception ex)
            {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                ex.Log(nameof(WebSocket));
#endif
                Error("An exception has occurred during the OnClose event.", ex);
            }
        }

        private void closeAsync(CloseEventArgs e, bool send, bool receive, bool received)
        {
            Action<CloseEventArgs, bool, bool, bool> closer = close;
            closer.BeginInvoke(e, send, receive, received, ar => closer.EndInvoke(ar), null);
        }

        private bool CloseHandshake(byte[] frameAsBytes, bool receive, bool received)
        {
            var sent = frameAsBytes != null && SendBytes(frameAsBytes);
            received = received ||
                       (receive && sent && _exitReceiving != null && _exitReceiving.WaitOne(_waitTime));

            var ret = sent && received;
#if COMPAT
            Log.DebugFormat("Was clean?: {0}\n  sent: {1}\n  received: {2}", ret, sent, received);
#else
            $"Was clean?: {ret}\n  sent: {sent}\n  received: {received}".Debug();
#endif

            return ret;
        }

        // As client
        private bool connect()
        {
            lock (_forState)
            {
                string msg;
                if (!checkIfAvailable(true, false, false, true, out msg))
                {
#if COMPAT
                        Log.Error(msg);
#else
                    msg.Error();
#endif
                    Error("An error has occurred in connecting.", null);

                    return false;
                }

                try
                {
                    _readyState = WebSocketState.Connecting;
                    if (!DoHandshake())
                        return false;

                    _readyState = WebSocketState.Open;
                }
                catch (Exception ex)
                {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                    ex.Log(nameof(WebSocket));
#endif
                    Fatal("An exception has occurred while connecting.", ex);

                    return false;
                }

                return true;
            }
        }

        // As client
        private string CreateExtensions()
        {
            var buff = new StringBuilder(80);

            if (_compression != CompressionMethod.None)
            {
                var str = _compression.ToExtensionString(
                    "server_no_context_takeover", "client_no_context_takeover");

                buff.AppendFormat("{0}, ", str);
            }

            var len = buff.Length;
            if (len > 2)
            {
                buff.Length = len - 2;
                return buff.ToString();
            }

            return null;
        }

        // As server
        private static HttpResponse CreateHandshakeFailureResponse(HttpStatusCode code)
        {
            var ret = HttpResponse.CreateCloseResponse(code);
            ret.Headers["Sec-WebSocket-Version"] = Version;

            return ret;
        }

        // As client
        private HttpRequest CreateHandshakeRequest()
        {
            var ret = HttpRequest.CreateWebSocketRequest(_uri);

            var headers = ret.Headers;
            if (!string.IsNullOrEmpty(_origin))
                headers["Origin"] = _origin;

            headers["Sec-WebSocket-Key"] = _base64Key;

            _protocolsRequested = _protocols != null;
            if (_protocolsRequested)
                headers["Sec-WebSocket-Protocol"] = string.Join(", ", _protocols);

            _extensionsRequested = _compression != CompressionMethod.None;
            if (_extensionsRequested)
                headers["Sec-WebSocket-Extensions"] = CreateExtensions();

            headers["Sec-WebSocket-Version"] = Version;

#if AUTHENTICATION
            AuthenticationResponse authRes = null;
            if (_authChallenge != null && _credentials != null)
            {
                authRes = new AuthenticationResponse(_authChallenge, _credentials, _nonceCount);
                _nonceCount = authRes.NonceCount;
            }
            else if (_preAuth)
            {
                authRes = new AuthenticationResponse(_credentials);
            }

            if (authRes != null)
                headers["Authorization"] = authRes.ToString();
#endif

            if (CookieCollection.Count > 0)
                ret.SetCookies(CookieCollection);

            return ret;
        }

        // As server
        private HttpResponse CreateHandshakeResponse()
        {
            var ret = HttpResponse.CreateWebSocketResponse();

            var headers = ret.Headers;
            headers["Sec-WebSocket-Accept"] = CreateResponseKey(_base64Key);

            if (_protocol != null)
                headers["Sec-WebSocket-Protocol"] = _protocol;

            if (_extensions != null)
                headers["Sec-WebSocket-Extensions"] = _extensions;

            if (CookieCollection.Count > 0)
                ret.SetCookies(CookieCollection);

            return ret;
        }

        // As server
        private bool CustomCheckHandshakeRequest(WebSocketContext context, out string message)
        {
            message = null;
            return CustomHandshakeRequestChecker == null
                   || (message = CustomHandshakeRequestChecker(context)) == null;
        }

        // As client
        private bool DoHandshake()
        {
            SetClientStream();
            var res = SendHandshakeRequest();

            string msg;
            if (!CheckHandshakeResponse(res, out msg))
            {
#if COMPAT
                        Log.Error(msg);
#else
                msg.Error();
#endif
                Fatal("An error has occurred while connecting.", CloseStatusCode.ProtocolError);

                return false;
            }

            if (_protocolsRequested)
                _protocol = res.Headers["Sec-WebSocket-Protocol"];

            if (_extensionsRequested)
                ProcessSecWebSocketExtensionsServerHeader(res.Headers["Sec-WebSocket-Extensions"]);

            ProcessCookies(res.Cookies);

            return true;
        }

        private void EnqueueToMessageEventQueue(MessageEventArgs e)
        {
            lock (_forMessageEventQueue) _messageEventQueue.Enqueue(e);
        }

        private void Error(string message, Exception exception) => OnError?.Invoke(this, new ConnectionFailureEventArgs(exception ?? new Exception(message)));

        private void Fatal(string message, Exception exception)
        {
            Fatal(message, (exception as WebSocketException)?.Code ?? CloseStatusCode.Abnormal);
        }

        private void Fatal(string message, CloseStatusCode code)
        {
            close(new CloseEventArgs(code, message), !code.IsReserved(), false, false);
        }

        private void Init()
        {
            _compression = CompressionMethod.None;
            CookieCollection = new CookieCollection();
            _forSend = new object();
            _forState = new object();
            _messageEventQueue = new Queue<MessageEventArgs>();
            _forMessageEventQueue = ((ICollection) _messageEventQueue).SyncRoot;
            _readyState = WebSocketState.Connecting;
        }

        private void Message()
        {
            MessageEventArgs e = null;
            lock (_forMessageEventQueue)
            {
                if (_inMessage || _messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
                    return;

                _inMessage = true;
                e = _messageEventQueue.Dequeue();
            }

            _message(e);
        }

        private void Messagec(MessageEventArgs e)
        {
            do
            {
                try
                {
                    OnMessage?.Invoke(this, e);
                }
                catch (Exception ex)
                {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                    ex.Log(nameof(WebSocket));
#endif
                    Error("An exception has occurred during an OnMessage event.", ex);
                }

                lock (_forMessageEventQueue)
                {
                    if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
                    {
                        _inMessage = false;
                        break;
                    }

                    e = _messageEventQueue.Dequeue();
                }
            } while (true);
        }

        private void Messages(MessageEventArgs e)
        {
            try
            {
                OnMessage?.Invoke(this, e);
            }
            catch (Exception ex)
            {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                ex.Log(nameof(WebSocket));
#endif
                Error("An exception has occurred during an OnMessage event.", ex);
            }

            lock (_forMessageEventQueue)
            {
                if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
                {
                    _inMessage = false;
                    return;
                }

                e = _messageEventQueue.Dequeue();
            }

            ThreadPool.QueueUserWorkItem(state => Messages(e));
        }

        private void Open()
        {
            _inMessage = true;
            StartReceiving();
            try
            {
                OnOpen?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                ex.Log(nameof(WebSocket));
#endif
                Error("An exception has occurred during the OnOpen event.", ex);
            }

            MessageEventArgs e = null;
            lock (_forMessageEventQueue)
            {
                if (_messageEventQueue.Count == 0 || _readyState != WebSocketState.Open)
                {
                    _inMessage = false;
                    return;
                }

                e = _messageEventQueue.Dequeue();
            }

            _message.BeginInvoke(e, ar => _message.EndInvoke(ar), null);
        }

        private bool ProcessCloseFrame(WebSocketFrame frame)
        {
            var payload = frame.PayloadData;
            close(new CloseEventArgs(payload), !payload.HasReservedCode, false, true);

            return false;
        }

        // As client
        private void ProcessCookies(CookieCollection cookies)
        {
            if (cookies.Count == 0)
                return;

            foreach (Cookie cookie in CookieCollection)
            {
                if (CookieCollection[cookie.Name] == null)
                {
                    if (!cookie.Expired)
                        CookieCollection.Add(cookie);

                    continue;
                }

                if (!cookie.Expired)
                {
                    CookieCollection.Add(cookie);
                    continue;
                }

                // TODO: Clear cookie
            }
        }

        private bool ProcessDataFrame(WebSocketFrame frame)
        {
            EnqueueToMessageEventQueue(
                frame.IsCompressed
                    ? new MessageEventArgs(
                        frame.Opcode, frame.PayloadData.ApplicationData.Compress(_compression, System.IO.Compression.CompressionMode.Decompress))
                    : new MessageEventArgs(frame));

            return true;
        }

        private bool ProcessFragmentFrame(WebSocketFrame frame)
        {
            if (!_inContinuation)
            {
                // Must process first fragment.
                if (frame.IsContinuation)
                    return true;

                _fragmentsOpcode = frame.Opcode;
                _fragmentsCompressed = frame.IsCompressed;
                _fragmentsBuffer = new MemoryStream();
                _inContinuation = true;
            }

            _fragmentsBuffer.WriteBytes(frame.PayloadData.ApplicationData, 1024);

            if (frame.IsFinal)
            {
                using (_fragmentsBuffer)
                {
                    var data = _fragmentsCompressed
                        ? _fragmentsBuffer.Compress(_compression, System.IO.Compression.CompressionMode.Decompress)
                        : _fragmentsBuffer;

                    EnqueueToMessageEventQueue(new MessageEventArgs(_fragmentsOpcode, data.ToArray()));
                }

                _fragmentsBuffer = null;
                _inContinuation = false;
            }

            return true;
        }

        private bool ProcessPingFrame(WebSocketFrame frame)
        {
            if (send(new WebSocketFrame(Opcode.Pong, frame.PayloadData, _client).ToArray()))
            {
#if COMPAT
                Log.Info("Returned a pong.");
#else
                "Returned a pong.".Info();
#endif
            }

            if (EmitOnPing)
                EnqueueToMessageEventQueue(new MessageEventArgs(frame));

            return true;
        }

        private bool ProcessPongFrame()
        {
            _receivePong.Set();
#if COMPAT
                Log.Info("Received a pong.");
#else
            "Received a pong.".Info();
#endif

            return true;
        }

        private bool ProcessReceivedFrame(WebSocketFrame frame)
        {
            string msg;
            if (!CheckReceivedFrame(frame, out msg))
                throw new WebSocketException(CloseStatusCode.ProtocolError, msg);

            frame.Unmask();
            return frame.IsFragment
                ? ProcessFragmentFrame(frame)
                : frame.IsData
                    ? ProcessDataFrame(frame)
                    : frame.IsPing
                        ? ProcessPingFrame(frame)
                        : frame.IsPong
                            ? ProcessPongFrame()
                            : frame.IsClose
                                ? ProcessCloseFrame(frame)
                                : ProcessUnsupportedFrame(frame);
        }

        // As server
        private void ProcessSecWebSocketExtensionsClientHeader(string value)
        {
            if (value == null)
                return;

            var buff = new StringBuilder(80);

            var comp = false;
            foreach (var e in value.SplitHeaderValue(','))
            {
                var ext = e.Trim();
                if (!comp && ext.IsCompressionExtension(CompressionMethod.Deflate))
                {
                    _compression = CompressionMethod.Deflate;
                    buff.AppendFormat(
                        "{0}, ",
                        _compression.ToExtensionString(
                            "client_no_context_takeover", "server_no_context_takeover"
                        )
                    );

                    comp = true;
                }
            }

            var len = buff.Length;
            if (len > 2)
            {
                buff.Length = len - 2;
                _extensions = buff.ToString();
            }
        }

        // As client
        private void ProcessSecWebSocketExtensionsServerHeader(string value)
        {
            if (value == null)
            {
                _compression = CompressionMethod.None;
                return;
            }

            _extensions = value;
        }

        // As server
        private void ProcessSecWebSocketProtocolHeader(IEnumerable<string> values)
        {
            if (values.Any(p => p == _protocol))
                return;

            _protocol = null;
        }

        private bool ProcessUnsupportedFrame(WebSocketFrame frame)
        {
#if COMPAT
            Log.Error("An unsupported frame:" + frame.PrintToString());
#else
            $"An unsupported frame: {frame.PrintToString()}".Error();
#endif

            Fatal("There is no way to handle it.", CloseStatusCode.PolicyViolation);

            return false;
        }

        // As client
        private void ReleaseClientResources()
        {
            _stream?.Dispose();
            _stream = null;

#if NET452
            _tcpClient?.Close();
#else
            _tcpClient?.Dispose();
#endif
            _tcpClient = null;
        }

        private void ReleaseCommonResources()
        {
            if (_fragmentsBuffer != null)
            {
                _fragmentsBuffer.Dispose();
                _fragmentsBuffer = null;
                _inContinuation = false;
            }

            if (_receivePong != null)
            {
#if NET452
                _receivePong.Close();
#else
                _receivePong.Dispose();
#endif
                _receivePong = null;
            }

            if (_exitReceiving != null)
            {
#if NET452
                _exitReceiving.Close();
#else
                _exitReceiving.Dispose();
#endif
                _exitReceiving = null;
            }
        }

        private void ReleaseResources()
        {
            if (_client)
                ReleaseClientResources();
            else
                ReleaseServerResources();

            ReleaseCommonResources();
        }

        // As server
        private void ReleaseServerResources()
        {
            if (_closeContext == null)
                return;

            _closeContext();
            _closeContext = null;
            _stream = null;
            _context = null;
        }

        private bool send(byte[] frameAsBytes)
        {
            lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
#if COMPAT
                    Log.Error("The sending has been interrupted.");
#else
                    "The sending has been interrupted.".Error();
#endif
                    return false;
                }

                return SendBytes(frameAsBytes);
            }
        }

        private bool send(Opcode opcode, Stream stream)
        {
            lock (_forSend)
            {
                var src = stream;
                var compressed = false;
                var sent = false;
                try
                {
                    if (_compression != CompressionMethod.None)
                    {
                        stream = stream.Compress(_compression);
                        compressed = true;
                    }

                    sent = send(opcode, stream, compressed);
                    if (!sent)
                        Error("The sending has been interrupted.", null);
                }
                catch (Exception ex)
                {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                    ex.Log(nameof(WebSocket));
#endif
                    Error("An exception has occurred while sending data.", ex);
                }
                finally
                {
                    if (compressed)
                        stream.Dispose();

                    src.Dispose();
                }

                return sent;
            }
        }

        private bool send(Opcode opcode, Stream stream, bool compressed)
        {
            var len = stream.Length;

            /* Not fragmented */

            if (len == 0)
                return send(Fin.Final, opcode, EmptyBytes, compressed);

            var quo = len/FragmentLength;
            var rem = (int) (len%FragmentLength);

            byte[] buff = null;
            if (quo == 0)
            {
                buff = new byte[rem];
                return stream.Read(buff, 0, rem) == rem &&
                       send(Fin.Final, opcode, buff, compressed);
            }

            buff = new byte[FragmentLength];
            if (quo == 1 && rem == 0)
                return stream.Read(buff, 0, FragmentLength) == FragmentLength &&
                       send(Fin.Final, opcode, buff, compressed);

            /* Send fragmented */

            // Begin
            if (stream.Read(buff, 0, FragmentLength) != FragmentLength ||
                !send(Fin.More, opcode, buff, compressed))
                return false;

            var n = rem == 0 ? quo - 2 : quo - 1;
            for (long i = 0; i < n; i++)
                if (stream.Read(buff, 0, FragmentLength) != FragmentLength ||
                    !send(Fin.More, Opcode.Cont, buff, compressed))
                    return false;

            // End
            if (rem == 0)
                rem = FragmentLength;
            else
                buff = new byte[rem];

            return stream.Read(buff, 0, rem) == rem && send(Fin.Final, Opcode.Cont, buff, compressed);
        }

        private bool send(Fin fin, Opcode opcode, byte[] data, bool compressed)
        {
            lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
#if COMPAT
                    Log.Error("The sending has been interrupted.");
#else
                    "The sending has been interrupted.".Error();
#endif
                    return false;
                }

                return SendBytes(new WebSocketFrame(fin, opcode, data, compressed, _client).ToArray());
            }
        }

        private Task SendAsync(Opcode opcode, Stream stream, Action<bool> completed)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var sent = send(opcode, stream);
                    completed?.Invoke(sent);
                }
                catch (Exception ex)
                {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                    ex.Log(nameof(WebSocket));
#endif
                    Error("An exception has occurred during a send callback.", ex);
                }
            });
        }

        private bool SendBytes(byte[] bytes)
        {
            try
            {
                _stream.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                ex.Log(nameof(WebSocket));
#endif
                return false;
            }
        }

        // As client
        private HttpResponse SendHandshakeRequest()
        {
            var req = CreateHandshakeRequest();
            var res = SendHttpRequest(req, 90000);

            if (res.IsUnauthorized)
            {
#if AUTHENTICATION
                var chal = res.Headers["WWW-Authenticate"];
                Log.Warn(String.Format("Received an authentication requirement for '{0}'.", chal));
                if (chal.IsNullOrEmpty())
                {
                    Log.Error("No authentication challenge is specified.");
                    return res;
                }

                _authChallenge = AuthenticationChallenge.Parse(chal);
                if (_authChallenge == null)
                {
                    Log.Error("An invalid authentication challenge is specified.");
                    return res;
                }

                if (_credentials != null &&
                    (!_preAuth || _authChallenge.Scheme == AuthenticationSchemes.Digest))
                {
                    if (res.HasConnectionClose)
                    {
                        releaseClientResources();
                        setClientStream();
                    }

                    var authRes = new AuthenticationResponse(_authChallenge, _credentials, _nonceCount);
                    _nonceCount = authRes.NonceCount;
                    req.Headers["Authorization"] = authRes.ToString();
                    res = sendHttpRequest(req, 15000);
                }
#else
                throw new InvalidOperationException("Authentication is not supported");
#endif
            }

            if (!res.IsRedirect) return res;

            var url = res.Headers["Location"];
#if COMPAT
                Log.WarnFormat("Received a redirection to '{0}'.", url);
#else
            $"Received a redirection to '{url}'.".Warn();
#endif

            if (_enableRedirection)
            {
                if (string.IsNullOrEmpty(url))
                {
#if COMPAT
                        Log.Error("No url to redirect is located.");
#else
                    "No url to redirect is located.".Error();
#endif
                    return res;
                }

                Uri uri;
                string msg;
                if (!url.TryCreateWebSocketUri(out uri, out msg))
                {
#if COMPAT
                        Log.Error("An invalid url to redirect is located: " + msg);
#endif
                    return res;
                }

                ReleaseClientResources();

                _uri = uri;
                IsSecure = uri.Scheme == "wss";

                SetClientStream();
                return SendHandshakeRequest();
            }

            return res;
        }

        // As client
        private HttpResponse SendHttpRequest(HttpRequest request, int millisecondsTimeout)
        {
#if COMPAT
            Log.DebugFormat("A request to the server:\n" + request);
#else
            $"A request to the server:\n {request.Stringify()}".Debug();
#endif
            var res = request.GetResponse(_stream, millisecondsTimeout);
#if COMPAT
            Log.DebugFormat("A response to this request:\n" + res);
#else
            $"A response to the server:\n {res.Stringify()}".Debug();
#endif

            return res;
        }

        // As server
        private bool SendHttpResponse(HttpResponse response)
        {
#if COMPAT
            Log.DebugFormat("A response to this request:\n" + response);
#else
            $"A response to the server:\n {response.Stringify()}".Debug();
#endif
            return SendBytes(response.ToByteArray());
        }

#if PROXY
// As client
        private void SendProxyConnectRequest()
        {
            var req = HttpRequest.CreateConnectRequest(_uri);
            var res = SendHttpRequest(req, 90000);

            if (res.IsProxyAuthenticationRequired)
            {
                var chal = res.Headers["Proxy-Authenticate"];
                Log.WarnFormat("Received a proxy authentication requirement for '{0}'.", chal);

                if (chal.IsNullOrEmpty())
                    throw new WebSocketException("No proxy authentication challenge is specified.");

                var authChal = AuthenticationChallenge.Parse(chal);
                if (authChal == null)
                    throw new WebSocketException("An invalid proxy authentication challenge is specified.");

                if (_proxyCredentials != null)
                {
                    if (res.HasConnectionClose)
                    {
                        releaseClientResources();
                        _tcpClient = new TcpClient(_proxyUri.DnsSafeHost, _proxyUri.Port);
                        _stream = _tcpClient.GetStream();
                    }

                    var authRes = new AuthenticationResponse(authChal, _proxyCredentials, 0);
                    req.Headers["Proxy-Authorization"] = authRes.ToString();
                    res = sendHttpRequest(req, 15000);
                }

                if (res.IsProxyAuthenticationRequired)
                    throw new WebSocketException("A proxy authentication is required.");
            }
            if (res.StatusCode[0] != '2')
                throw new WebSocketException(
                    "The proxy has failed a connection to the requested host and port.");
        }  
#endif

        // As client
        private void SetClientStream()
        {
#if PROXY
            if (_proxyUri != null)
            {
#if NET452
                _tcpClient = new TcpClient(_proxyUri.DnsSafeHost, _proxyUri.Port);
#else
                _tcpClient = new TcpClient();
#endif
                _stream = _tcpClient.GetStream();
                SendProxyConnectRequest();
            }
            else
#endif
            {
#if NET452
                _tcpClient = new TcpClient(_uri.DnsSafeHost, _uri.Port);
#else
                _tcpClient = new TcpClient();

                _tcpClient.ConnectAsync(_uri.DnsSafeHost, _uri.Port).Wait();
#endif
                _stream = _tcpClient.GetStream();
            }

#if SSL
            if (_secure)
            {
                var conf = SslConfiguration;
                var host = conf.TargetHost;
                if (host != _uri.DnsSafeHost)
                    throw new WebSocketException(
                      CloseStatusCode.TlsHandshakeFailure, "An invalid host name is specified.");

                try
                {
                    var sslStream = new SslStream(
                      _stream,
                      false,
                      conf.ServerCertificateValidationCallback,
                      conf.ClientCertificateSelectionCallback);

                    sslStream.AuthenticateAsClient(
                      host,
                      conf.ClientCertificates,
                      conf.EnabledSslProtocols,
                      conf.CheckCertificateRevocation);

                    _stream = sslStream;
                }
                catch (Exception ex)
                {
                    throw new WebSocketException(CloseStatusCode.TlsHandshakeFailure, ex);
                }
            }
#endif
        }

        private void StartReceiving()
        {
            if (_messageEventQueue.Count > 0)
                _messageEventQueue.Clear();

            _exitReceiving = new AutoResetEvent(false);
            _receivePong = new AutoResetEvent(false);

            Action receive = null;
            receive =
                () =>
                    WebSocketFrame.ReadFrameAsync(
                        _stream,
                        false,
                        frame =>
                        {
                            if (!ProcessReceivedFrame(frame) || _readyState == WebSocketState.Closed)
                            {
                                _exitReceiving?.Set();

                                return;
                            }

                            // Receive next asap because the Ping or Close needs a response to it.
                            receive();

                            if (_inMessage || !HasMessage || _readyState != WebSocketState.Open)
                                return;

                            Message();
                        },
                        ex =>
                        {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                            ex.Log(nameof(WebSocket));
#endif
                            Fatal("An exception has occurred while receiving.", ex);
                        }
                    );

            receive();
        }

        // As client
        private bool ValidateSecWebSocketAcceptHeader(string value)
        {
            return value != null && value.TrimStart() == CreateResponseKey(_base64Key);
        }

        // As client
        private bool ValidateSecWebSocketExtensionsServerHeader(string value)
        {
            if (value == null)
                return true;

            if (value.Length == 0 || !_extensionsRequested)
                return false;

            var comp = _compression != CompressionMethod.None;
            foreach (var e in value.SplitHeaderValue(','))
            {
                var ext = e.Trim();
                if (comp && ext.IsCompressionExtension(_compression))
                {
                    if (!ext.Contains("server_no_context_takeover"))
                    {
#if COMPAT
                        Log.Error("The server hasn't sent back 'server_no_context_takeover'.");
#else
                        "The server hasn't sent back 'server_no_context_takeover'.".Error();
#endif
                        return false;
                    }

#if COMPAT
                    if (!ext.Contains("client_no_context_takeover"))
                        Log.Info("The server hasn't sent back 'client_no_context_takeover'.");
#endif

                    var method = _compression.ToExtensionString();
                    var invalid =
                        ext.SplitHeaderValue(';').Any(
                            t =>
                            {
                                t = t.Trim();
                                return t != method
                                       && t != "server_no_context_takeover"
                                       && t != "client_no_context_takeover";
                            }
                        );

                    if (invalid)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }


        // As server
        private static bool ValidateSecWebSocketKeyHeader(string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        private static bool ValidateSecWebSocketProtocolClientHeader(string value)
        {
            return value == null || value.Length > 0;
        }

        // As client
        private bool ValidateSecWebSocketProtocolServerHeader(string value)
        {
            if (value == null)
                return !_protocolsRequested;

            if (value.Length == 0)
                return false;

            return _protocolsRequested && _protocols.Any(p => p == value);
        }

        // As server
        private static bool ValidateSecWebSocketVersionClientHeader(string value)
        {
            return value != null && value == Version;
        }

        // As client
        private static bool ValidateSecWebSocketVersionServerHeader(string value)
        {
            return value == null || value == Version;
        }

        #endregion

        #region Internal Methods

        internal static string CheckCloseParameters(CloseStatusCode code, string reason, bool client)
        {
            return code == CloseStatusCode.NoStatus
                ? (!string.IsNullOrEmpty(reason) ? "NoStatus cannot have a reason." : null)
                : code == CloseStatusCode.MandatoryExtension && !client
                    ? "MandatoryExtension cannot be used by a server."
                    : code == CloseStatusCode.ServerError && client
                        ? "ServerError cannot be used by a client."
                        : !string.IsNullOrEmpty(reason) && Encoding.UTF8.GetBytes(reason).Length > 123
                            ? "A reason has greater than the allowable max size."
                            : null;
        }

        internal static bool CheckParametersForClose(
            CloseStatusCode code, string reason, bool client, out string message
        )
        {
            message = null;

            if (code == CloseStatusCode.NoStatus && !string.IsNullOrEmpty(reason))
            {
                message = "'code' cannot have a reason.";
                return false;
            }

            if (code == CloseStatusCode.MandatoryExtension && !client)
            {
                message = "'code' cannot be used by a server.";
                return false;
            }

            if (code == CloseStatusCode.ServerError && client)
            {
                message = "'code' cannot be used by a client.";
                return false;
            }

            if (!string.IsNullOrEmpty(reason) && Encoding.UTF8.GetBytes(reason).Length > 123)
            {
                message = "The size of 'reason' is greater than the allowable max size.";
                return false;
            }

            return true;
        }

        internal static string CheckPingParameter(string message, out byte[] bytes)
        {
            bytes = Encoding.UTF8.GetBytes(message);
            return bytes.Length > 125 ? "A message has greater than the allowable max size." : null;
        }

        internal static string CheckSendParameter(byte[] data)
        {
            return data == null ? "'data' is null." : null;
        }

        internal static string CheckSendParameter(FileInfo file)
        {
            return file == null ? "'file' is null." : null;
        }

        internal static string CheckSendParameter(string data)
        {
            return data == null ? "'data' is null." : null;
        }

        internal static string CheckSendParameters(Stream stream, int length)
        {
            return stream == null
                ? "'stream' is null."
                : !stream.CanRead
                    ? "'stream' cannot be read."
                    : length < 1
                        ? "'length' is less than 1."
                        : null;
        }

        // As server
        internal void Close(HttpResponse response)
        {
            _readyState = WebSocketState.Closing;

            SendHttpResponse(response);
            ReleaseServerResources();

            _readyState = WebSocketState.Closed;
        }

        // As server
        internal void Close(HttpStatusCode code)
        {
            Close(CreateHandshakeFailureResponse(code));
        }

        // As server
        internal void Close(CloseEventArgs e, byte[] frameAsBytes, bool receive)
        {
            lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
#if COMPAT
                    Log.Info("The closing is already in progress.");
#endif
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
#if COMPAT
                    Log.Info("The connection has been closed.");
#endif
                    return;
                }

                _readyState = WebSocketState.Closing;
            }

            e.WasClean = CloseHandshake(frameAsBytes, receive, false);
            ReleaseServerResources();
            ReleaseCommonResources();

            _readyState = WebSocketState.Closed;

            try
            {
                OnClose?.Invoke(this, e);
            }
            catch (Exception ex)
            {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                ex.Log(nameof(WebSocket));
#endif
            }
        }

        // As client
        internal static string CreateBase64Key()
        {
            var src = new byte[16];
            RandomNumber.GetBytes(src);

            return Convert.ToBase64String(src);
        }

        internal static string CreateResponseKey(string base64Key)
        {
            var buff = new StringBuilder(base64Key, 64);
            buff.Append(Guid);
            var sha1 = SHA1.Create();
            var src = sha1.ComputeHash(Encoding.UTF8.GetBytes(buff.ToString()));

            return Convert.ToBase64String(src);
        }

        // As server
        internal void InternalAccept()
        {
            try
            {
                if (!AcceptHandshake())
                    return;

                _readyState = WebSocketState.Open;
            }
            catch (Exception ex)
            {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                ex.Log(nameof(WebSocket));
#endif
                Fatal("An exception has occurred while accepting.", ex);

                return;
            }

            Open();
        }

        internal bool Ping(byte[] frameAsBytes, TimeSpan timeout)
        {
            if (_readyState != WebSocketState.Open || !send(frameAsBytes))
                return false;

            return _receivePong != null && _receivePong.WaitOne(timeout);
        }

        // As server, used to broadcast
        internal void Send(Opcode opcode, byte[] data, Dictionary<CompressionMethod, byte[]> cache)
        {
            lock (_forSend)
            {
                lock (_forState)
                {
                    if (_readyState != WebSocketState.Open)
                    {
#if COMPAT
                        Log.Error("The sending has been interrupted.");
#else
                        "The sending has been interrupted.".Error();
#endif
                        return;
                    }

                    try
                    {
                        byte[] found;
                        if (!cache.TryGetValue(_compression, out found))
                        {
                            found =
                                new WebSocketFrame(
                                        Fin.Final,
                                        opcode,
                                        data.Compress(_compression),
                                        _compression != CompressionMethod.None,
                                        false
                                    )
                                    .ToArray();

                            cache.Add(_compression, found);
                        }

                        SendBytes(found);
                    }
                    catch (Exception ex)
                    {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                        ex.Log(nameof(WebSocket));
#endif
                    }
                }
            }
        }

        // As server, used to broadcast
        internal void Send(Opcode opcode, Stream stream, Dictionary<CompressionMethod, Stream> cache)
        {
            lock (_forSend)
            {
                try
                {
                    Stream found;
                    if (!cache.TryGetValue(_compression, out found))
                    {
                        found = stream.Compress(_compression);
                        cache.Add(_compression, found);
                    }
                    else
                    {
                        found.Position = 0;
                    }

                    send(opcode, found, _compression != CompressionMethod.None);
                }
                catch (Exception ex)
                {
#if COMPAT
                    Log.Error(ex.ToString());
#else
                    ex.Log(nameof(WebSocket));
#endif
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Accepts the WebSocket handshake request.
        /// </summary>
        /// <remarks>
        /// This method isn't available in a client.
        /// </remarks>
        public void Accept()
        {
            string msg;
            if (!checkIfAvailable(false, true, true, false, false, false, out msg))
            {
#if COMPAT
                        Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in accepting.", null);

                return;
            }

            if (accept())
                Open();
        }

        /// <summary>
        /// Accepts the WebSocket handshake request asynchronously.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method doesn't wait for the accept to be complete.
        ///   </para>
        ///   <para>
        ///   This method isn't available in a client.
        ///   </para>
        /// </remarks>
        public void AcceptAsync()
        {
            string msg;
            if (!checkIfAvailable(false, true, true, false, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in accepting.", null);

                return;
            }

            Func<bool> acceptor = accept;
            acceptor.BeginInvoke(
                ar =>
                {
                    if (acceptor.EndInvoke(ar))
                        Open();
                },
                null
            );
        }

        /// <summary>
        /// Closes the WebSocket connection, and releases all associated resources.
        /// </summary>
        public void Close()
        {
            string msg;
            if (!checkIfAvailable(true, true, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            close(new CloseEventArgs(), true, true, false);
        }

        /// <summary>
        /// Closes the WebSocket connection with the specified <paramref name="code"/>,
        /// and releases all associated resources.
        /// </summary>
        /// <param name="code">
        /// One of the <see cref="CloseStatusCode"/> enum values that represents
        /// the status code indicating the reason for the close.
        /// </param>
        public void Close(CloseStatusCode code)
        {
            string msg;
            if (!checkIfAvailable(true, true, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (!CheckParametersForClose(code, null, _client, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (code == CloseStatusCode.NoStatus)
            {
                close(new CloseEventArgs(), true, true, false);
                return;
            }

            var send = !code.IsReserved();
            close(new CloseEventArgs(code), send, send, false);
        }

        /// <summary>
        /// Closes the WebSocket connection with the specified <paramref name="code"/> and
        /// <paramref name="reason"/>, and releases all associated resources.
        /// </summary>
        /// <param name="code">
        /// One of the <see cref="CloseStatusCode"/> enum values that represents
        /// the status code indicating the reason for the close.
        /// </param>
        /// <param name="reason">
        /// A <see cref="string"/> that represents the reason for the close.
        /// The size must be 123 bytes or less.
        /// </param>
        public void Close(CloseStatusCode code, string reason)
        {
            string msg;
            if (!checkIfAvailable(true, true, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (!CheckParametersForClose(code, reason, _client, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (code == CloseStatusCode.NoStatus)
            {
                close(new CloseEventArgs(), true, true, false);
                return;
            }

            var send = !code.IsReserved();
            close(new CloseEventArgs(code, reason), send, send, false);
        }

        /// <summary>
        /// Closes the WebSocket connection asynchronously, and releases
        /// all associated resources.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the close to be complete.
        /// </remarks>
        public void CloseAsync()
        {
            string msg;
            if (!checkIfAvailable(true, true, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            closeAsync(new CloseEventArgs(), true, true, false);
        }

        /// <summary>
        /// Closes the WebSocket connection asynchronously with the specified
        /// <paramref name="code"/>, and releases all associated resources.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the close to be complete.
        /// </remarks>
        /// <param name="code">
        /// One of the <see cref="CloseStatusCode"/> enum values that represents
        /// the status code indicating the reason for the close.
        /// </param>
        public void CloseAsync(CloseStatusCode code)
        {
            string msg;
            if (!checkIfAvailable(true, true, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (!CheckParametersForClose(code, null, _client, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (code == CloseStatusCode.NoStatus)
            {
                closeAsync(new CloseEventArgs(), true, true, false);
                return;
            }

            var send = !code.IsReserved();
            closeAsync(new CloseEventArgs(code), send, send, false);
        }

        /// <summary>
        /// Closes the WebSocket connection asynchronously with the specified
        /// <paramref name="code"/> and <paramref name="reason"/>, and releases
        /// all associated resources.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the close to be complete.
        /// </remarks>
        /// <param name="code">
        /// One of the <see cref="CloseStatusCode"/> enum values that represents
        /// the status code indicating the reason for the close.
        /// </param>
        /// <param name="reason">
        /// A <see cref="string"/> that represents the reason for the close.
        /// The size must be 123 bytes or less.
        /// </param>
        public void CloseAsync(CloseStatusCode code, string reason)
        {
            string msg;
            if (!checkIfAvailable(true, true, false, false, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (!CheckParametersForClose(code, reason, _client, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in closing the connection.", null);

                return;
            }

            if (code == CloseStatusCode.NoStatus)
            {
                closeAsync(new CloseEventArgs(), true, true, false);
                return;
            }

            var send = !code.IsReserved();
            closeAsync(new CloseEventArgs(code, reason), send, send, false);
        }

        /// <summary>
        /// Establishes a WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method isn't available in a server.
        /// </remarks>
        public void Connect()
        {
            string msg;
            if (!checkIfAvailable(true, false, true, false, false, true, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in connecting.", null);

                return;
            }

            if (connect())
                Open();
        }

        /// <summary>
        /// Establishes a WebSocket connection asynchronously.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method doesn't wait for the connect to be complete.
        ///   </para>
        ///   <para>
        ///   This method isn't available in a server.
        ///   </para>
        /// </remarks>
        public void ConnectAsync()
        {
            string msg;
            if (!checkIfAvailable(true, false, true, false, false, true, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in connecting.", null);

                return;
            }

            Func<bool> connector = connect;
            connector.BeginInvoke(
                ar =>
                {
                    if (connector.EndInvoke(ar))
                        Open();
                },
                null
            );
        }

        /// <summary>
        /// Sends a ping using the WebSocket connection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the <see cref="WebSocket"/> receives a pong to this ping in a time;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Ping()
        {
            var bytes = _client
                ? WebSocketFrame.CreatePingFrame(true).ToArray()
                : WebSocketFrame.EmptyPingBytes;

            return Ping(bytes, _waitTime);
        }

        /// <summary>
        /// Sends a ping with the specified <paramref name="message"/> using the WebSocket connection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the <see cref="WebSocket"/> receives a pong to this ping in a time;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="message">
        /// A <see cref="string"/> that represents the message to send.
        /// </param>
        public bool Ping(string message)
        {
            if (string.IsNullOrEmpty(message))
                return Ping();

            byte[] data;
            var msg = CheckPingParameter(message, out data);
            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending a ping.", null);

                return false;
            }

            return Ping(WebSocketFrame.CreatePingFrame(data, _client).ToArray(), _waitTime);
        }

        private static string CheckIfAvailable(WebSocketState state, bool connecting, bool open, bool closing,
            bool closed)
        {
            return (!connecting && state == WebSocketState.Connecting) ||
                   (!open && state == WebSocketState.Open) ||
                   (!closing && state == WebSocketState.Closing) ||
                   (!closed && state == WebSocketState.Closed)
                ? "This operation isn't available in: " + state.ToString().ToLower()
                : null;
        }

        /// <summary>
        /// Sends binary <paramref name="data"/> using the WebSocket connection.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        public void Send(byte[] data)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameter(data);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            send(Opcode.Binary, new MemoryStream(data));
        }

        /// <summary>
        /// Sends the specified <paramref name="file"/> as binary data using the WebSocket connection.
        /// </summary>
        /// <param name="file">
        /// A <see cref="FileInfo"/> that represents the file to send.
        /// </param>
        public void Send(FileInfo file)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameter(file);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            send(Opcode.Binary, file.OpenRead());
        }

        /// <summary>
        /// Sends text <paramref name="data"/> using the WebSocket connection.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        public void Send(string data)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameter(data);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            send(Opcode.Text, new MemoryStream(Encoding.UTF8.GetBytes(data)));
        }

        /// <summary>
        /// Sends binary <paramref name="data"/> asynchronously using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that represents the binary data to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        public void SendAsync(byte[] data, Action<bool> completed)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameter(data);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            SendAsync(Opcode.Binary, new MemoryStream(data), completed);
        }

        /// <summary>
        /// Sends the specified <paramref name="file"/> as binary data asynchronously using
        /// the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="file">
        /// A <see cref="FileInfo"/> that represents the file to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        public void SendAsync(FileInfo file, Action<bool> completed)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameter(file);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            SendAsync(Opcode.Binary, file.OpenRead(), completed);
        }

        /// <summary>
        /// Sends text <paramref name="data"/> asynchronously using the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that represents the text data to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        public void SendAsync(string data, Action<bool> completed)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameter(data);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            SendAsync(Opcode.Text, new MemoryStream(Encoding.UTF8.GetBytes(data)), completed);
        }

        /// <summary>
        /// Sends binary data from the specified <see cref="Stream"/> asynchronously using
        /// the WebSocket connection.
        /// </summary>
        /// <remarks>
        /// This method doesn't wait for the send to be complete.
        /// </remarks>
        /// <param name="stream">
        /// A <see cref="Stream"/> from which contains the binary data to send.
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that represents the number of bytes to send.
        /// </param>
        /// <param name="completed">
        /// An <c>Action&lt;bool&gt;</c> delegate that references the method(s) called when
        /// the send is complete. A <see cref="bool"/> passed to this delegate is <c>true</c>
        /// if the send is complete successfully.
        /// </param>
        public void SendAsync(Stream stream, int length, Action<bool> completed)
        {
            var msg = CheckIfAvailable(_readyState, false, true, false, false) ??
                      CheckSendParameters(stream, length);

            if (msg != null)
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in sending data.", null);

                return;
            }

            stream.ReadBytesAsync(
                length,
                data =>
                {
                    var len = data.Length;
                    if (len == 0)
                    {
#if COMPAT
                        Log.Error("The data cannot be read from 'stream'.");
#endif
                        Error("An error has occurred in sending data.", null);

                        return;
                    }

#if COMPAT
                    if (len < length)
                        Log.InfoFormat(
                            "The length of the data is less than 'length':\n  expected: {0}\n  actual: {1}",
                            length,
                            len);
#endif
                    var sent = send(Opcode.Binary, new MemoryStream(data));
                    completed?.Invoke(sent);
                },
                ex =>
                {
                    Error("An exception has occurred while sending data.", ex);
                }
            );
        }

        /// <summary>
        /// Sets an HTTP <paramref name="cookie"/> to send with
        /// the WebSocket handshake request to the server.
        /// </summary>
        /// <param name="cookie">
        /// A <see cref="Cookie"/> that represents a cookie to send.
        /// </param>
        public void SetCookie(Cookie cookie)
        {
            string msg;
            if (!checkIfAvailable(true, false, true, false, false, true, out msg))
            {
#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in setting a cookie.", null);

                return;
            }

            if (cookie == null)
            {
#if COMPAT
                Log.Error("'cookie' is null.");
#endif
                Error("An error has occurred in setting a cookie.", null);

                return;
            }

            lock (_forState)
            {
                if (!checkIfAvailable(true, false, false, true, out msg))
                {
#if COMPAT
                Log.Error(msg);
#else
                    msg.Error();
#endif
                    Error("An error has occurred in setting a cookie.", null);

                    return;
                }

                // TODO: lock (CookieCollection.SyncRoot)
                CookieCollection.Add(cookie);
            }
        }

#if AUTHENTICATION
/// <summary>
/// Sets a pair of <paramref name="username"/> and <paramref name="password"/> for
/// the HTTP authentication (Basic/Digest).
/// </summary>
/// <param name="username">
///   <para>
///   A <see cref="string"/> that represents the user name used to authenticate.
///   </para>
///   <para>
///   If <paramref name="username"/> is <see langword="null"/> or empty,
///   the credentials will be initialized and not be sent.
///   </para>
/// </param>
/// <param name="password">
/// A <see cref="string"/> that represents the password for
/// <paramref name="username"/> used to authenticate.
/// </param>
/// <param name="preAuth">
/// <c>true</c> if the <see cref="WebSocket"/> sends the credentials for
/// the Basic authentication with the first handshake request to the server;
/// otherwise, <c>false</c>.
/// </param>
        public void SetCredentials(string username, string password, bool preAuth)
        {
            string msg;
            if (!checkIfAvailable(true, false, true, false, false, true, out msg))
            {
                Log.Error(msg);
                Error("An error has occurred in setting the credentials.", null);

                return;
            }

            if (!CheckParametersForSetCredentials(username, password, out msg))
            {
                Log.Error(msg);
                Error("An error has occurred in setting the credentials.", null);

                return;
            }

            lock (_forState)
            {
                if (!checkIfAvailable(true, false, false, true, out msg))
                {
                    Log.Error(msg);
                    Error("An error has occurred in setting the credentials.", null);

                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    Log.WarnFormat("The credentials are initialized.");
                    Credentials = null;
                    _preAuth = false;

                    return;
                }

                Credentials = new NetworkCredential(username, password, _uri.PathAndQuery);
                _preAuth = preAuth;
            }
        }
#endif
#if PROXY
/// <summary>
/// Sets the HTTP proxy server URL to connect through, and if necessary,
/// a pair of <paramref name="username"/> and <paramref name="password"/> for
/// the proxy server authentication (Basic/Digest).
/// </summary>
/// <param name="url">
///   <para>
///   A <see cref="string"/> that represents the HTTP proxy server URL to
///   connect through. The syntax must be http://&lt;host&gt;[:&lt;port&gt;].
///   </para>
///   <para>
///   If <paramref name="url"/> is <see langword="null"/> or empty,
///   the url and credentials for the proxy will be initialized,
///   and the <see cref="WebSocket"/> will not use the proxy to
///   connect through.
///   </para>
/// </param>
/// <param name="username">
///   <para>
///   A <see cref="string"/> that represents the user name used to authenticate.
///   </para>
///   <para>
///   If <paramref name="username"/> is <see langword="null"/> or empty,
///   the credentials for the proxy will be initialized and not be sent.
///   </para>
/// </param>
/// <param name="password">
/// A <see cref="string"/> that represents the password for
/// <paramref name="username"/> used to authenticate.
/// </param>
        public void SetProxy(string url, string username, string password)
        {
            string msg;
            if (!checkIfAvailable(true, false, true, false, false, true, out msg))
            {

#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in setting the proxy.", null);

                return;
            }

            if (!CheckParametersForSetProxy(url, username, password, out msg))
            {

#if COMPAT
                Log.Error(msg);
#else
                msg.Error();
#endif
                Error("An error has occurred in setting the proxy.", null);

                return;
            }

            lock (_forState)
            {
                if (!checkIfAvailable(true, false, false, true, out msg))
                {
#if COMPAT
                Log.Error(msg);
#else
                    msg.Error();
#endif
                    Error("An error has occurred in setting the proxy.", null);

                    return;
                }

                if (string.IsNullOrEmpty(url))
                {
#if COMPAT
                    Log.WarnFormat("The url and credentials for the proxy are initialized.");
#endif
                    _proxyUri = null;
                    _proxyCredentials = null;

                    return;
                }

                _proxyUri = new Uri(url);

                if (string.IsNullOrEmpty(username))
                {
#if COMPAT
                    Log.WarnFormat("The credentials for the proxy are initialized.");
#endif
                    _proxyCredentials = null;

                    return;
                }

                _proxyCredentials =
                    new NetworkCredential(username, password, $"{_uri.DnsSafeHost}:{_uri.Port}");
            }
        }
#endif

        #endregion

        #region Explicit Interface Implementations

        /// <summary>
        /// Closes the WebSocket connection, and releases all associated resources.
        /// </summary>
        /// <remarks>
        /// This method closes the connection with <see cref="CloseStatusCode.Away"/>.
        /// </remarks>
        void IDisposable.Dispose()
        {
            close(new CloseEventArgs(CloseStatusCode.Away), true, true, false);
        }

        #endregion
    }
}

#endif
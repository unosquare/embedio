namespace Unosquare.Net
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Labs.EmbedIO;
    using Labs.EmbedIO.Constants;
    using Swan;

    /// <summary>
    /// Implements the WebSocket interface.
    /// </summary>
    /// <remarks>
    /// The WebSocket class provides a set of methods and properties for two-way communication using
    /// the WebSocket protocol (<see href="http://tools.ietf.org/html/rfc6455">RFC 6455</see>).
    /// </remarks>
    public class WebSocket : IDisposable
    {
        internal const string Version = "13";

        // Represents the empty array of <see cref="byte"/> used internally.
        internal static readonly byte[] EmptyBytes = new byte[0];

        // Represents the length used to determine whether the data should be fragmented in sending.
        internal static readonly int FragmentLength = 1016;

        /// <summary>
        /// Represents the random number generator used internally.
        /// </summary>
        internal static readonly RandomNumberGenerator RandomNumber = RandomNumberGenerator.Create();

        private const string Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private readonly Action<MessageEventArgs> _message;
        private readonly object _forState = new object();
        private readonly Queue<MessageEventArgs> _messageEventQueue = new Queue<MessageEventArgs>();
        private readonly object _forMessageEventQueue;
        private readonly WebSocketValidator _validator;

        private string _base64Key;

        private CompressionMethod _compression = CompressionMethod.None;
        private volatile WebSocketState _readyState = WebSocketState.Connecting;
        private WebSocketContext _context;
        private bool _enableRedirection;
        private AutoResetEvent _exitReceiving;
        private string _extensions;
        private MemoryStream _fragmentsBuffer;
        private bool _fragmentsCompressed;
        private Opcode _fragmentsOpcode;
        private volatile bool _inMessage;
        private string _origin;
        private AutoResetEvent _receivePong;
#if SSL
        private ClientSslConfiguration _sslConfig;
#endif
        private Stream _stream;
        private TcpClient _tcpClient;
        private Uri _uri;
        private TimeSpan _waitTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocket" /> class with
        /// the specified WebSocket URL.
        /// </summary>
        /// <param name="url">A <see cref="string" /> that represents the WebSocket URL to connect.</param>
        /// <exception cref="System.ArgumentNullException">url.</exception>
        /// <exception cref="System.ArgumentException">
        /// An empty string. - url
        /// or
        /// url
        /// or
        /// protocols.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="url" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><para>
        ///   <paramref name="url" /> is invalid.
        /// </para>
        /// <para>
        /// -or-
        /// </para></exception>
        public WebSocket(string url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (url.Length == 0)
                throw new ArgumentException("An empty string.", nameof(url));

            if (!url.TryCreateWebSocketUri(out _uri, out var msg))
                throw new ArgumentException(msg, nameof(url));

            _base64Key = CreateBase64Key();
            IsClient = true;

            _message = Messagec;
#if SSL
            IsSecure = _uri.Scheme == "wss";
#endif
            _waitTime = TimeSpan.FromSeconds(5);
            _forMessageEventQueue = ((ICollection) _messageEventQueue).SyncRoot;
            _validator = new WebSocketValidator(this);
        }

        // As server
        internal WebSocket(WebSocketContext context)
        {
            _context = context;

            _message = Messages;
#if SSL
            IsSecure = context.IsSecureConnection;
#endif
            _stream = context.Stream;
            _waitTime = TimeSpan.FromSeconds(1);
            _forMessageEventQueue = ((ICollection) _messageEventQueue).SyncRoot;
            _validator = new WebSocketValidator(this);
        }

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

        /// <summary>
        /// Gets or sets the compression method used to compress a message on the WebSocket connection.
        /// </summary>
        /// <value>
        /// One of the <see cref="CompressionMethod"/> enum values, specifies the compression method
        /// used to compress a message. The default value is <see cref="CompressionMethod.None"/>.
        /// </value>
        public CompressionMethod Compression
        {
            get => _compression;

            set
            {
                lock (_forState)
                {
                    if (!_validator.CheckIfAvailable(true, false, true, false, false))
                    {
                        Error("An error has occurred in setting the compression.");

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
                {
                    foreach (var cookie in CookieCollection)
                        yield return cookie;
                }
            }
        }

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
            get => _enableRedirection;

            set
            {
                lock (_forState)
                {
                    if (!_validator.CheckIfAvailable(true, false, true, false, false))
                    {
                        Error("An error has occurred in setting the enable redirection.");
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
        public bool IsAlive => PingAsync().Result; // TODO: Change?

#if SSL
/// <summary>
/// Gets a value indicating whether the WebSocket connection is secure.
/// </summary>
/// <value>
/// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
/// </value>
        public bool IsSecure { get; private set; }
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
        ///   <c>&lt;scheme&gt;://&lt;host&gt;[:&lt;port&gt;]</c>.
        ///   </para>
        /// </value>
        public string Origin
        {
            get => _origin;

            set
            {
                lock (_forState)
                {
                    if (!_validator.CheckIfAvailable(true, false, true, false, false))
                    {
                        Error("An error has occurred in setting the origin.");
                        return;
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        _origin = value;
                        return;
                    }

                    if (!Uri.TryCreate(value, UriKind.Absolute, out var origin) || origin.Segments.Length > 1)
                    {
                        "The syntax of an origin must be '<scheme>://<host>[:<port>]'.".Error();
                        Error("An error has occurred in setting the origin.");

                        return;
                    }

                    _origin = value.TrimEnd('/');
                }
            }
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
        public Uri Url => IsClient ? _uri : _context.RequestUri;

        /// <summary>
        /// Gets or sets the wait time for the response to the Ping or Close.
        /// </summary>
        /// <value>
        /// A <see cref="TimeSpan"/> that represents the wait time. The default value is the same as
        /// 5 seconds, or 1 second if the <see cref="WebSocket"/> is used in a server.
        /// </value>
        public TimeSpan WaitTime
        {
            get => _waitTime;

            set
            {
                lock (_forState)
                {
                    if (value == TimeSpan.Zero || !_validator.CheckIfAvailable(true, true, true, false, false))
                    {
                        Error("An error has occurred in setting the wait time.");
                        return;
                    }

                    _waitTime = value;
                }
            }
        }

        internal bool InContinuation { get; private set; }

        internal bool IsClient { get; }

        internal bool IsExtensionsRequested { get; private set; }

        internal CookieCollection CookieCollection { get; } = new CookieCollection();

        internal bool HasMessage
        {
            get
            {
                lock (_forMessageEventQueue)
                {
                    return _messageEventQueue.Count > 0;
                }
            }
        }

        // As server
        internal bool IgnoreExtensions { get; set; } = true;

        internal bool IsConnected => _readyState == WebSocketState.Open || _readyState == WebSocketState.Closing;

        /// <summary>
        /// Closes the WebSocket connection asynchronously, and releases
        /// all associated resources.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous closes websocket connection.
        /// </returns>
        public async Task CloseAsync(
            CloseStatusCode code = CloseStatusCode.Undefined, 
            string reason = null,
            CancellationToken ct = default)
        {
            if (!_validator.CheckIfAvailable())
            {
                Error("An error has occurred in closing the connection.");
                return;
            }

            if (code != CloseStatusCode.Undefined &&
                !WebSocketValidator.CheckParametersForClose(code, reason, IsClient))
            {
                Error("An error has occurred in closing the connection.");
                return;
            }

            if (code == CloseStatusCode.NoStatus)
            {
                await InternalCloseAsync(new CloseEventArgs(), ct: ct);
                return;
            }

            var send = !IsOpcodeReserved(code);
            await InternalCloseAsync(new CloseEventArgs(code, reason), send, send, ct: ct);
        }

        /// <summary>
        /// Establishes a WebSocket connection asynchronously.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// If CheckIfAvailable statement terminates execution of the method; otherwise, 
        /// establishes a WebSocket connection.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method doesn't wait for the connect to be complete.
        /// </para>
        /// <para>
        /// This method isn't available in a server.
        /// </para></remarks>
        public async Task ConnectAsync(CancellationToken ct = default)
        {
            if (!_validator.CheckIfAvailable(true, false, true, false, false))
            {
                Error("An error has occurred in connecting.");

                return;
            }

            try
            {
                lock (_forState)
                {
                    _readyState = WebSocketState.Connecting;
                }

                var handShake = await DoHandshakeAsync();

                if (!handShake)
                    return;

                lock (_forState)
                {
                    _readyState = WebSocketState.Open;
                }

                Open();
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
                Fatal("An exception has occurred while connecting.", ex);
            }
        }

        /// <summary>
        /// Sends a ping using the WebSocket connection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the <see cref="WebSocket"/> receives a pong to this ping in a time;
        /// otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> PingAsync()
        {
            var bytes = IsClient
                ? WebSocketFrame.CreatePingFrame(true).ToArray()
                : WebSocketFrame.EmptyPingBytes;

            return PingAsync(bytes, _waitTime);
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
        public async Task<bool> PingAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                return await PingAsync();

            var data = Encoding.UTF8.GetBytes(message);

            if (data.Length <= 125)
                return await PingAsync(WebSocketFrame.CreatePingFrame(data, IsClient).ToArray(), _waitTime);

            "A message has greater than the allowable max size.".Error();
            Error("An error has occurred in sending a ping.");

            return false;
        }

        /// <summary>
        /// Sends binary <paramref name="data" /> using the WebSocket connection.
        /// </summary>
        /// <param name="data">An array of <see cref="byte" /> that represents the binary data to send.</param>
        /// <param name="opcode">The opcode.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous of send 
        /// binary data using websocket.
        /// </returns>
        public async Task SendAsync(byte[] data, Opcode opcode, CancellationToken ct = default)
        {
            var msg = WebSocketValidator.CheckIfAvailable(_readyState);

            if (msg != null)
            {
                msg.Error();
                Error("An error has occurred in sending data.");

                return;
            }

            WebSocketStream stream = null;

            try
            {
                stream = new WebSocketStream(data, opcode, _compression, IsClient);

                // TODO: add async
                foreach (var frame in stream.GetFrames())
                    Send(frame);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
                Error("An error has occurred in sending data.", ex);
            }
            finally
            {
                stream?.Dispose();
            }
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
            if (cookie == null) return;

            lock (_forState)
            {
                if (!_validator.CheckIfAvailable(true, false, false, true))
                {
                    Error("An error has occurred in setting a cookie.");

                    return;
                }
            }

            lock (CookieCollection.SyncRoot)
                CookieCollection.Add(cookie);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // TODO: this is correct?
            InternalCloseAsync(new CloseEventArgs(CloseStatusCode.Away)).Wait();
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
        internal async Task CloseAsync(HttpResponse response)
        {
            lock (_forState)
            {
                _readyState = WebSocketState.Closing;
            }

            await SendHttpResponseAsync(response);
            ReleaseServerResources();

            lock (_forState)
            {
                _readyState = WebSocketState.Closed;
            }
        }

        // As server
        internal async Task CloseAsync(CloseEventArgs e, byte[] frameAsBytes, bool receive,
            CancellationToken ct = default)
        {
            lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
                    "The closing is already in progress.".Debug();
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
                    "The connection has been closed.".Debug();
                    return;
                }

                _readyState = WebSocketState.Closing;
            }

            // TODO: Fix
            e.WasClean = await CloseHandshakeAsync(frameAsBytes, receive, false, ct).ConfigureAwait(false);
            ReleaseServerResources();
            ReleaseCommonResources();

            _readyState = WebSocketState.Closed;

            try
            {
                OnClose?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
            }
        }

        // As client
        internal bool ValidateSecWebSocketAcceptHeader(string value) =>
            value?.TrimStart() == CreateResponseKey(_base64Key);

        // As server
        internal async Task InternalAcceptAsync()
        {
            try
            {
                var handShake = await AcceptHandshakeAsync();

                if (handShake == false)
                    return;

                _readyState = WebSocketState.Open;
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
                Fatal("An exception has occurred while accepting.", ex);

                return;
            }

            Open();
        }

        internal async Task<bool> PingAsync(byte[] frameAsBytes, TimeSpan timeout)
        {
            if (_readyState != WebSocketState.Open)
                return false;

            await _stream.WriteAsync(frameAsBytes, 0, frameAsBytes.Length);

            return _receivePong != null && _receivePong.WaitOne(timeout);
        }

        // As server
        private static HttpResponse CreateHandshakeFailureResponse(HttpStatusCode code)
        {
            var ret = HttpResponse.CreateCloseResponse(code);
            ret.Headers["Sec-WebSocket-Version"] = Version;

            return ret;
        }

        private static bool IsOpcodeReserved(CloseStatusCode code) => code == CloseStatusCode.Undefined ||
                                                                      code == CloseStatusCode.NoStatus ||
                                                                      code == CloseStatusCode.Abnormal ||
                                                                      code == CloseStatusCode.TlsHandshakeFailure;

        // As server
        private async Task<bool> AcceptHandshakeAsync()
        {
            $"A request from {_context.UserEndPoint}:\n{_context}".Debug();

            if (!_validator.CheckHandshakeRequest(_context, out string msg))
            {
                await SendHttpResponseAsync(CreateHandshakeFailureResponse(HttpStatusCode.BadRequest));

                msg.Error();
                Fatal("An error has occurred while accepting.", CloseStatusCode.ProtocolError);

                return false;
            }

            _base64Key = _context.Headers["Sec-WebSocket-Key"];

            if (!IgnoreExtensions)
                ProcessSecWebSocketExtensionsClientHeader(_context.Headers["Sec-WebSocket-Extensions"]);

            await SendHttpResponseAsync(CreateHandshakeResponse());
            return true;
        }

        // As client
        private async Task InternalCloseAsync(
            CloseEventArgs e,
            bool send = true,
            bool receive = true,
            bool received = false,
            CancellationToken ct = default)
        {
            lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
                    "The closing is already in progress.".Info();
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
                    "The connection has been closed.".Info();
                    return;
                }

                send = send && _readyState == WebSocketState.Open;
                receive = receive && send;

                _readyState = WebSocketState.Closing;
            }

            "Begin closing the connection.".Info();

            var bytes = send ? WebSocketFrame.CreateCloseFrame(e.PayloadData, IsClient).ToArray() : null;
            e.WasClean = await CloseHandshakeAsync(bytes, receive, received, ct);
            ReleaseResources();

            "End closing the connection.".Info();

            lock (_forState)
            {
                _readyState = WebSocketState.Closed;
            }

            try
            {
                OnClose?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
                Error("An exception has occurred during the OnClose event.", ex);
            }
        }

        private async Task<bool> CloseHandshakeAsync(
            byte[] frameAsBytes, 
            bool receive, 
            bool received,
            CancellationToken ct)
        {
            var sent = frameAsBytes != null;

            if (sent)
            {
                await _stream.WriteAsync(frameAsBytes, 0, frameAsBytes.Length, ct);
            }

            received = received ||
                       (receive && sent && _exitReceiving != null && _exitReceiving.WaitOne(_waitTime));

            var ret = sent && received;
            $"Was clean?: {ret}\n  sent: {sent}\n  received: {received}".Trace();

            return ret;
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

            if (len <= 2) return null;

            buff.Length = len - 2;
            return buff.ToString();
        }

        // As client
        private HttpRequest CreateHandshakeRequest()
        {
            var ret = HttpRequest.CreateWebSocketRequest(_uri);

            var headers = ret.Headers;
            if (!string.IsNullOrEmpty(_origin))
                headers["Origin"] = _origin;

            headers["Sec-WebSocket-Key"] = _base64Key;

            IsExtensionsRequested = _compression != CompressionMethod.None;

            if (IsExtensionsRequested)
                headers["Sec-WebSocket-Extensions"] = CreateExtensions();

            headers["Sec-WebSocket-Version"] = Version;

            ret.SetCookies(CookieCollection);

            return ret;
        }

        // As server
        private HttpResponse CreateHandshakeResponse()
        {
            var ret = HttpResponse.CreateWebSocketResponse();

            var headers = ret.Headers;
            headers["Sec-WebSocket-Accept"] = CreateResponseKey(_base64Key);

            if (_extensions != null)
                headers["Sec-WebSocket-Extensions"] = _extensions;

            ret.SetCookies(CookieCollection);

            return ret;
        }

        // As client
        private async Task<bool> DoHandshakeAsync()
        {
            await SetClientStream();
            var res = await SendHandshakeRequestAsync();

            if (!_validator.CheckHandshakeResponse(res, out var msg))
            {
                msg.Error();
                Fatal("An error has occurred while connecting.", CloseStatusCode.ProtocolError);

                return false;
            }

            if (IsExtensionsRequested)
                ProcessSecWebSocketExtensionsServerHeader(res.Headers["Sec-WebSocket-Extensions"]);

            ProcessCookies(res.Cookies);

            return true;
        }

        private void EnqueueToMessageEventQueue(MessageEventArgs e)
        {
            lock (_forMessageEventQueue)
            {
                _messageEventQueue.Enqueue(e);
            }
        }

        private void Error(string message, Exception exception = null)
            => OnError?.Invoke(this, new ConnectionFailureEventArgs(exception ?? new Exception(message)));

        private void Fatal(string message, Exception exception = null) => Fatal(message,
            (exception as WebSocketException)?.Code ?? CloseStatusCode.Abnormal);

        private void Fatal(string message, CloseStatusCode code) =>
            InternalCloseAsync(new CloseEventArgs(code, message), !IsOpcodeReserved(code), false).Wait();

        private void Message()
        {
            MessageEventArgs e;
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
                    ex.Log(nameof(WebSocket));
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
                ex.Log(nameof(WebSocket));
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

            Task.Factory.StartNew(() => Messages(e));
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
                ex.Log(nameof(WebSocket));
                Error("An exception has occurred during the OnOpen event.", ex);
            }

            MessageEventArgs e;
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
            InternalCloseAsync(new CloseEventArgs(payload), !payload.HasReservedCode, false, true).Wait();

            return false;
        }

        // As client
        private void ProcessCookies(ICollection cookies)
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
                }

                // TODO: Clear cookie
            }
        }

        private bool ProcessDataFrame(WebSocketFrame frame)
        {
            EnqueueToMessageEventQueue(
                frame.IsCompressed
                    ? new MessageEventArgs(
                        frame.Opcode,
                        frame.PayloadData.ApplicationData.Compress(_compression,
                            System.IO.Compression.CompressionMode.Decompress))
                    : new MessageEventArgs(frame));

            return true;
        }

        private bool ProcessFragmentFrame(WebSocketFrame frame)
        {
            if (!InContinuation)
            {
                // Must process first fragment.
                if (frame.IsContinuation)
                    return true;

                _fragmentsOpcode = frame.Opcode;
                _fragmentsCompressed = frame.IsCompressed;
                _fragmentsBuffer = new MemoryStream();
                InContinuation = true;
            }

            using (var input = new MemoryStream(frame.PayloadData.ApplicationData))
                input.CopyTo(_fragmentsBuffer, 1024);

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
                InContinuation = false;
            }

            return true;
        }

        private bool ProcessPingFrame(WebSocketFrame frame)
        {
            if (Send(new WebSocketFrame(Opcode.Pong, frame.PayloadData, IsClient)))
            {
                "Returned a pong.".Info();
            }

            if (EmitOnPing)
                EnqueueToMessageEventQueue(new MessageEventArgs(frame));

            return true;
        }

        private bool ProcessPongFrame()
        {
            _receivePong.Set();
            "Received a pong.".Info();

            return true;
        }

        private bool ProcessReceivedFrame(WebSocketFrame frame)
        {
            _validator.CheckReceivedFrame(frame);

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
            foreach (var e in value.SplitHeaderValue(Strings.CommaSplitChar))
            {
                var ext = e.Trim();

                if (!comp && ext.IsCompressionExtension(CompressionMethod.Deflate))
                {
                    _compression = CompressionMethod.Deflate;
                    buff.AppendFormat(
                        "{0}, ",
                        _compression.ToExtensionString(
                            "client_no_context_takeover", "server_no_context_takeover"));

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

        private bool ProcessUnsupportedFrame(WebSocketFrame frame)
        {
            $"An unsupported frame: {frame.PrintToString()}".Error();
            Fatal("There is no way to handle it.", CloseStatusCode.PolicyViolation);

            return false;
        }

        // As client
        private void ReleaseClientResources()
        {
            _stream?.Dispose();
            _stream = null;

#if NET46
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
                InContinuation = false;
            }

            if (_receivePong != null)
            {
#if NET46
                _receivePong.Close();
#else
                _receivePong.Dispose();
#endif
                _receivePong = null;
            }

            if (_exitReceiving != null)
            {
#if NET46
                _exitReceiving.Close();
#else
                _exitReceiving.Dispose();
#endif
                _exitReceiving = null;
            }
        }

        private void ReleaseResources()
        {
            if (IsClient)
                ReleaseClientResources();
            else
                ReleaseServerResources();

            ReleaseCommonResources();
        }

        // As server
        private void ReleaseServerResources()
        {
            if (IsClient)
                return;

            _context.CloseAsync();
            _stream = null;
            _context = null;
        }

        private bool Send(WebSocketFrame frame)
        {
            lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
                    "The sending has been interrupted.".Error();
                    return false;
                }
            }

            var frameAsBytes = frame.ToArray();
            _stream.Write(frameAsBytes, 0, frameAsBytes.Length);
            return true;
        }

        private async Task<bool> Send(Opcode opcode, Stream stream, CancellationToken ct)
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

                sent = await Send(opcode, stream, compressed, ct);
                if (!sent)
                    Error("The sending has been interrupted.");
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
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

        private async Task<bool> Send(Opcode opcode, Stream stream, bool compressed, CancellationToken ct)
        {
            var len = stream.Length;

            /* Not fragmented */

            if (len == 0)
                return Send(Fin.Final, opcode, EmptyBytes, compressed, ct);

            var quo = len / FragmentLength;
            var rem = (int) (len % FragmentLength);

            byte[] buff;

            if (quo == 0)
            {
                buff = new byte[rem];
                return stream.Read(buff, 0, rem) == rem &&
                       Send(Fin.Final, opcode, buff, compressed, ct);
            }

            buff = new byte[FragmentLength];
            if (quo == 1 && rem == 0)
            {
                return stream.Read(buff, 0, FragmentLength) == FragmentLength &&
                       Send(Fin.Final, opcode, buff, compressed, ct);
            }

            /* Send fragmented */

            // Begin
            if (stream.Read(buff, 0, FragmentLength) != FragmentLength ||
                !Send(Fin.More, opcode, buff, compressed, ct))
                return false;

            var n = rem == 0 ? quo - 2 : quo - 1;
            for (long i = 0; i < n; i++)
            {
                if (stream.Read(buff, 0, FragmentLength) != FragmentLength ||
                    !Send(Fin.More, Opcode.Cont, buff, compressed, ct))
                {
                    return false;
                }
            }

            // End
            if (rem == 0)
                rem = FragmentLength;
            else
                buff = new byte[rem];

            return stream.Read(buff, 0, rem) == rem && Send(Fin.Final, Opcode.Cont, buff, compressed, ct);
        }

        private bool Send(Fin fin, Opcode opcode, byte[] data, bool compressed, CancellationToken ct)
        {
            lock (_forState)
            {
                if (_readyState == WebSocketState.Open)
                    return SendBytes(new WebSocketFrame(fin, opcode, data, compressed, IsClient).ToArray(), ct);

                "The sending has been interrupted.".Error();
                return false;

            }
        }

        private bool SendBytes(byte[] bytes, CancellationToken ct)
        {
            try
            {
                // TODO: Use async here
                _stream.Write(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocket));
                return false;
            }
        }

        // As client
        private async Task<HttpResponse> SendHandshakeRequestAsync()
        {
            while (true)
            {
                var req = CreateHandshakeRequest();
                var res = await req.GetResponse(_stream);

                if (res.IsUnauthorized)
                {
                    throw new InvalidOperationException("Authentication is not supported");
                }

                if (!res.IsRedirect) return res;

                var url = res.Headers["Location"];
                $"Received a redirection to '{url}'.".Warn();

                if (!_enableRedirection) return res;

                if (string.IsNullOrEmpty(url))
                {
                    "No url to redirect is located.".Error();
                    return res;
                }

                if (!url.TryCreateWebSocketUri(out var uri, out var msg))
                {
                    $"An invalid url to redirect is located: {msg}".Error();
                    return res;
                }

                ReleaseClientResources();

                _uri = uri;
#if SSL
                IsSecure = uri.Scheme == "wss";
#endif

                await SetClientStream();
            }
        }

        // As server
        private Task SendHttpResponseAsync(HttpBase response)
        {
            var bytes = response.ToByteArray();

            return _stream.WriteAsync(bytes, 0, bytes.Length);
        }

        // As client
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task SetClientStream()
        {
#if NET46
            _tcpClient = new TcpClient(_uri.DnsSafeHost, _uri.Port);
#else
            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(_uri.DnsSafeHost, _uri.Port);
#endif
            _stream = _tcpClient.GetStream();

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
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        private void StartReceiving()
        {
            if (_messageEventQueue.Count > 0)
                _messageEventQueue.Clear();

            _exitReceiving = new AutoResetEvent(false);
            _receivePong = new AutoResetEvent(false);

            async void Receive()
            {
                try
                {
                    var frame = await WebSocketFrame.ReadFrameAsync(_stream);
                    var result = ProcessReceivedFrame(frame);

                    if (!result || _readyState == WebSocketState.Closed)
                    {
                        _exitReceiving?.Set();

                        return;
                    }

                    // Receive next asap because the Ping or Close needs a response to it.
                    Receive();

                    if (_inMessage || !HasMessage || _readyState != WebSocketState.Open)
                        return;

                    Message();
                }
                catch (Exception ex)
                {
                    Fatal("An exception has occurred while receiving.", ex);
                }
            }

            Receive();
        }
    }
}
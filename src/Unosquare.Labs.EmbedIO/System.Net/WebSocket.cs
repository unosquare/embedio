using System.Linq;

namespace Unosquare.Net
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
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
        internal static readonly byte[] EmptyBytes = new byte[0];

        private readonly Action<MessageEventArgs> _message;
        private readonly object _forState = new object();
        private readonly ConcurrentQueue<MessageEventArgs> _messageEventQueue = new ConcurrentQueue<MessageEventArgs>();
        private readonly WebSocketValidator _validator;

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

            WebSocketKey = new WebSocketKey(true);
            IsClient = true;

            _message = Messagec;
#if SSL
            IsSecure = _uri.Scheme == "wss";
#endif
            _waitTime = TimeSpan.FromSeconds(5);
            _validator = new WebSocketValidator(this);
        }

        // As server
        internal WebSocket(WebSocketContext context)
        {
            _context = context;

            _message = Messages;
            WebSocketKey = new WebSocketKey(false);

#if SSL
            IsSecure = context.IsSecureConnection;
#endif
            _stream = context.Stream;
            _waitTime = TimeSpan.FromSeconds(1);
            _validator = new WebSocketValidator(this);
        }

        /// <summary>
        /// Occurs when the <see cref="WebSocket"/> receives a message.
        /// </summary>
        public event EventHandler<MessageEventArgs> OnMessage;

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
                        return;

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
                        return;

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
                        return;

                    if (string.IsNullOrEmpty(value))
                    {
                        _origin = value;
                        return;
                    }

                    if (!Uri.TryCreate(value, UriKind.Absolute, out var origin) || origin.Segments.Length > 1)
                    {
                        "The syntax of an origin must be '<scheme>://<host>[:<port>]'.".Error();

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
                        return;

                    _waitTime = value;
                }
            }
        }

        internal bool InContinuation { get; private set; }

        internal bool IsClient { get; }

        internal bool IsExtensionsRequested { get; set; }

        internal CookieCollection CookieCollection { get; } = new CookieCollection();

        // As server
        internal bool IgnoreExtensions { get; set; } = true;

        internal bool IsConnected
        {
            get
            {
                lock (_forState)
                {
                    return _readyState == WebSocketState.Open || _readyState == WebSocketState.Closing;
                }
            }
        }

        internal WebSocketKey WebSocketKey { get; }

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
                return;

            if (code != CloseStatusCode.Undefined &&
                !WebSocketValidator.CheckParametersForClose(code, reason, IsClient))
                return;

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
                return;

            try
            {
                lock (_forState)
                {
                    _readyState = WebSocketState.Connecting;
                }

                await DoHandshakeAsync();

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
            if (_readyState != WebSocketState.Open)
                throw new WebSocketException(CloseStatusCode.Normal, $"This operation isn\'t available in: {_readyState.ToString()}");

            WebSocketStream stream = null;

            try
            {
                stream = new WebSocketStream(data, opcode, _compression, IsClient);

                // TODO: add async
                foreach (var frame in stream.GetFrames())
                    Send(frame);
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
                    return;
            }

            lock (CookieCollection.SyncRoot)
                CookieCollection.Add(cookie);
        }

        /// <inheritdoc />
        public void Dispose() => InternalCloseAsync(new CloseEventArgs(CloseStatusCode.Away)).Wait();

        // As server
        internal async Task InternalAcceptAsync()
        {
            try
            {
                $"A request from {_context.UserEndPoint}:\n{_context}".Debug();

                _validator.ThrowIfInvalid(_context);

                WebSocketKey.KeyValue = _context.Headers["Sec-WebSocket-Key"];

                if (!IgnoreExtensions)
                    ProcessSecWebSocketExtensionsClientHeader(_context.Headers["Sec-WebSocket-Extensions"]);

                await SendHandshakeAsync();

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

        private static bool IsOpcodeReserved(CloseStatusCode code) => code == CloseStatusCode.Undefined ||
                                                                      code == CloseStatusCode.NoStatus ||
                                                                      code == CloseStatusCode.Abnormal ||
                                                                      code == CloseStatusCode.TlsHandshakeFailure;

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

            return sent && received;
        }

        // As client
        private async Task DoHandshakeAsync()
        {
            await SetClientStream();
            var res = await SendHandshakeRequestAsync();

            _validator.ThrowIfInvalidResponse(res);

            if (IsExtensionsRequested)
                ProcessSecWebSocketExtensionsServerHeader(res.Headers["Sec-WebSocket-Extensions"]);

            CookieCollection.AddRange(res.Cookies.Where(y => !y.Expired));
        }

        private void Fatal(string message, Exception exception = null) => Fatal(message,
            (exception as WebSocketException)?.Code ?? CloseStatusCode.Abnormal);

        private void Fatal(string message, CloseStatusCode code) =>
            InternalCloseAsync(new CloseEventArgs(code, message), !IsOpcodeReserved(code), false).Wait();

        private void Message()
        {
            if (_inMessage || _messageEventQueue.IsEmpty || _readyState != WebSocketState.Open)
                return;

            _inMessage = true;

            if (_messageEventQueue.TryDequeue(out var e))
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
                }

                if (_messageEventQueue.TryDequeue(out e) && _readyState == WebSocketState.Open) continue;

                _inMessage = false;
                break;
            }
            while (true);
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
            }

            if (!_messageEventQueue.TryDequeue(out e) || _readyState != WebSocketState.Open)
            {
                _inMessage = false;
                return;
            }

            Task.Factory.StartNew(() => Messages(e));
        }

        private void Open()
        {
            _inMessage = true;
            StartReceiving();

            if (!_messageEventQueue.TryDequeue(out var e) || _readyState != WebSocketState.Open)
            {
                _inMessage = false;
                return;
            }

            _message.BeginInvoke(e, ar => _message.EndInvoke(ar), null);
        }

        private void ProcessCloseFrame(WebSocketFrame frame)
        {
            var payload = frame.PayloadData;
            InternalCloseAsync(new CloseEventArgs(payload), !payload.HasReservedCode, false, true).Wait();
        }

        private void ProcessDataFrame(WebSocketFrame frame)
        {
            _messageEventQueue.Enqueue(
                frame.IsCompressed
                    ? new MessageEventArgs(
                        frame.Opcode,
                        frame.PayloadData.ApplicationData.Compress(_compression,
                            System.IO.Compression.CompressionMode.Decompress))
                    : new MessageEventArgs(frame));
        }

        private bool ProcessFragmentFrame(WebSocketFrame frame)
        {
            if (!InContinuation)
            {
                // Must process first fragment.
                if (frame.Opcode == Opcode.Cont)
                    return true;

                _fragmentsOpcode = frame.Opcode;
                _fragmentsCompressed = frame.IsCompressed;
                _fragmentsBuffer = new MemoryStream();
                InContinuation = true;
            }

            using (var input = new MemoryStream(frame.PayloadData.ApplicationData))
                input.CopyTo(_fragmentsBuffer, 1024);

            if (frame.Fin == Fin.Final)
            {
                using (_fragmentsBuffer)
                {
                    var data = _fragmentsCompressed
                        ? _fragmentsBuffer.Compress(_compression, System.IO.Compression.CompressionMode.Decompress)
                        : _fragmentsBuffer;

                    _messageEventQueue.Enqueue(new MessageEventArgs(_fragmentsOpcode, data.ToArray()));
                }

                _fragmentsBuffer = null;
                InContinuation = false;
            }

            return true;
        }

        private void ProcessPingFrame(WebSocketFrame frame)
        {
            Send(new WebSocketFrame(Opcode.Pong, frame.PayloadData, IsClient));

            if (EmitOnPing)
                _messageEventQueue.Enqueue(new MessageEventArgs(frame));
        }

        private void ProcessPongFrame()
        {
            _receivePong.Set();
            "Received a pong.".Info();
        }

        private bool ProcessReceivedFrame(WebSocketFrame frame)
        {
            if (frame.IsFragment)
                return ProcessFragmentFrame(frame);

            switch (frame.Opcode)
            {
                case Opcode.Text:
                case Opcode.Binary:
                    ProcessDataFrame(frame);
                    break;
                case Opcode.Ping:
                    ProcessPingFrame(frame);
                    break;
                case Opcode.Pong:
                    ProcessPongFrame();
                    break;
                case Opcode.Close:
                    ProcessCloseFrame(frame);
                    break;
                default:
                    $"An unsupported frame: {frame.PrintToString()}".Error();
                    Fatal("There is no way to handle it.", CloseStatusCode.PolicyViolation);
                    return false;
            }

            return true;
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

        private void Send(WebSocketFrame frame)
        {
            lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
                    "The sending has been interrupted.".Error();
                    return;
                }
            }

            var frameAsBytes = frame.ToArray();
            _stream.Write(frameAsBytes, 0, frameAsBytes.Length);
        }

        // As client
        private async Task<HttpResponse> SendHandshakeRequestAsync()
        {
            while (true)
            {
                var req = HttpRequest.CreateHandshakeRequest(this);
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
        private Task SendHandshakeAsync()
        {
            var ret = HttpResponse.CreateWebSocketResponse();

            var headers = ret.Headers;
            headers["Sec-WebSocket-Accept"] = WebSocketKey.CreateResponseKey();

            if (_extensions != null)
                headers["Sec-WebSocket-Extensions"] = _extensions;

            ret.SetCookies(CookieCollection);

            var bytes = ret.ToByteArray();

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
            while (_messageEventQueue.TryDequeue(out _))
            {
                // do nothing
            }

            _exitReceiving = new AutoResetEvent(false);
            _receivePong = new AutoResetEvent(false);

            var frameStream = new WebSocketFrameStream(_stream);

            async void Receive()
            {
                try
                {
                    var frame = await frameStream.ReadFrameAsync(this);

                    if (frame == null)
                        return;

                    var result = ProcessReceivedFrame(frame);

                    if (!result || _readyState == WebSocketState.Closed)
                    {
                        _exitReceiving?.Set();

                        return;
                    }

                    // Receive next asap because the Ping or Close needs a response to it.
                    Receive();

                    if (_inMessage || _messageEventQueue.IsEmpty || _readyState != WebSocketState.Open)
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
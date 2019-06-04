namespace Unosquare.Net
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
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
    internal class WebSocket : IWebSocket
    {
        private readonly object _forState = new object();
        private readonly ConcurrentQueue<MessageEventArgs> _messageEventQueue = new ConcurrentQueue<MessageEventArgs>();
        private readonly WebSocketValidator _validator;

        private CompressionMethod _compression = CompressionMethod.None;
        private volatile WebSocketState _readyState = WebSocketState.Connecting;
        private WebSocketContext _context;
        private bool _enableRedirection;
        private AutoResetEvent _exitReceiving;
        private string _extensions;
        private FragmentBuffer _fragmentsBuffer;
        private volatile bool _inMessage;
        private string _origin;
        private AutoResetEvent _receivePong;
        private Stream _stream;
        private TimeSpan _waitTime;

        // As server
        internal WebSocket(WebSocketContext context)
        {
            _context = context;

            WebSocketKey = new WebSocketKey();

            IsSecure = context.IsSecureConnection;
            _stream = context.Stream;
            _waitTime = TimeSpan.FromSeconds(1);
            _validator = new WebSocketValidator(this);
        }

        internal event EventHandler<MessageEventArgs> OnMessage;

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
                    if (!_validator.CheckIfAvailable(false))
                        return;

                    _compression = value;
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
        /// Gets a value indicating whether the WebSocket connection is secure.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        public bool IsSecure { get; }

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
                    if (!_validator.CheckIfAvailable(false))
                        return;

                    if (string.IsNullOrEmpty(value))
                    {
                        _origin = value;
                        return;
                    }

                    if (!Uri.TryCreate(value, UriKind.Absolute, out var origin) || origin.Segments.Length > 1)
                    {
                        "The syntax of an origin must be '<scheme>://<host>[:<port>]'.".Error(nameof(Origin));

                        return;
                    }

                    _origin = value.TrimEnd('/');
                }
            }
        }

        /// <inheritdoc />
        public WebSocketState State => _readyState;

        /// <summary>
        /// Gets the WebSocket URL used to connect, or accepted.
        /// </summary>
        /// <value>
        /// A <see cref="Uri"/> that represents the URL used to connect, or accepted.
        /// </value>
        public Uri Url => _context.RequestUri;
        
        internal bool InContinuation { get; private set; }

        internal CookieCollection CookieCollection { get; } = new CookieCollection();

        // As server
        internal bool IgnoreExtensions { get; set; } = true;

        internal WebSocketKey WebSocketKey { get; }

        /// <inheritdoc />
        public Task SendAsync(byte[] buffer, bool isText, CancellationToken ct) => SendAsync(buffer, isText ? Opcode.Text : Opcode.Binary, ct);

        /// <inheritdoc />
        public Task CloseAsync(CancellationToken cancellationToken = default) => CloseAsync(CloseStatusCode.Normal, cancellationToken: cancellationToken);

        /// <inheritdoc />
        public Task CloseAsync(
            CloseStatusCode code = CloseStatusCode.Undefined,
            string reason = null,
            CancellationToken cancellationToken = default)
        {
            if (!_validator.CheckIfAvailable())
                return Task.Delay(0, cancellationToken);

            if (code != CloseStatusCode.Undefined &&
                !WebSocketValidator.CheckParametersForClose(code, reason))
            {
                return Task.Delay(0, cancellationToken);
            }

            if (code == CloseStatusCode.NoStatus)
                return InternalCloseAsync(ct: cancellationToken);

            var send = !IsOpcodeReserved(code);
            return InternalCloseAsync(new PayloadData((ushort)code, reason), send, send, cancellationToken);
        }

        /// <summary>
        /// Sends a ping using the WebSocket connection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the <see cref="WebSocket"/> receives a pong to this ping in a time;
        /// otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> PingAsync() => PingAsync(WebSocketFrame.EmptyPingBytes, _waitTime);

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
        public Task<bool> PingAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                return PingAsync();

            var data = Encoding.UTF8.GetBytes(message);

            if (data.Length <= 125)
                return PingAsync(WebSocketFrame.CreatePingFrame(data).ToArray(), _waitTime);

            "A message has greater than the allowable max size.".Error(nameof(PingAsync));

            return Task.FromResult(false);
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
                stream = new WebSocketStream(data, opcode, _compression);

                foreach (var frame in stream.GetFrames())
                    await Send(frame).ConfigureAwait(false);
            }
            finally
            {
                stream?.Dispose();
            }
        }

        /// <inheritdoc />
        void IDisposable.Dispose()
        {
            try
            {
                InternalCloseAsync(new PayloadData((ushort)CloseStatusCode.Away)).Wait();
            }
            catch
            {
                // Ignored
            }
        }

        internal async Task InternalAcceptAsync()
        {
            try
            {
                _validator.ThrowIfInvalid(_context);

                WebSocketKey.KeyValue = _context.Headers[HttpHeaderNames.SecWebSocketKey];

                if (!IgnoreExtensions)
                    ProcessSecWebSocketExtensionsClientHeader(_context.Headers[HttpHeaderNames.SecWebSocketExtensions]);

                await SendHandshakeAsync().ConfigureAwait(false);

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

            await _stream.WriteAsync(frameAsBytes, 0, frameAsBytes.Length).ConfigureAwait(false);

            return _receivePong != null && _receivePong.WaitOne(timeout);
        }

        private static bool IsOpcodeReserved(CloseStatusCode code) => code == CloseStatusCode.Undefined ||
                                                                      code == CloseStatusCode.NoStatus ||
                                                                      code == CloseStatusCode.Abnormal ||
                                                                      code == CloseStatusCode.TlsHandshakeFailure;

        private async Task InternalCloseAsync(
            PayloadData payloadData = null,
            bool send = true,
            bool receive = true,
            CancellationToken ct = default)
        {
            lock (_forState)
            {
                if (_readyState == WebSocketState.Closing)
                {
                    "The closing is already in progress.".Trace(nameof(InternalCloseAsync));
                    return;
                }

                if (_readyState == WebSocketState.Closed)
                {
                    "The connection has been closed.".Trace(nameof(InternalCloseAsync));
                    return;
                }

                send = send && _readyState == WebSocketState.Open;
                receive = receive && send;

                _readyState = WebSocketState.Closing;
            }

            "Begin closing the connection.".Trace(nameof(InternalCloseAsync));

            var bytes = send ? WebSocketFrame.CreateCloseFrame(payloadData).ToArray() : null;
            await CloseHandshakeAsync(bytes, receive, ct).ConfigureAwait(false);
            ReleaseResources();

            "End closing the connection.".Trace(nameof(InternalCloseAsync));

            lock (_forState)
            {
                _readyState = WebSocketState.Closed;
            }
        }

        private async Task CloseHandshakeAsync(byte[] frameAsBytes,
                                               bool receive,
                                               CancellationToken ct)
        {
            var sent = frameAsBytes != null;

            if (sent)
            {
                await _stream.WriteAsync(frameAsBytes, 0, frameAsBytes.Length, ct).ConfigureAwait(false);
            }

            if (receive && sent)
                _exitReceiving?.WaitOne(_waitTime);
        }

        private void Fatal(string message, Exception exception = null) => Fatal(message,
            (exception as WebSocketException)?.Code ?? CloseStatusCode.Abnormal);

        private void Fatal(string message, CloseStatusCode code) =>
            InternalCloseAsync(new PayloadData((ushort)code, message), !IsOpcodeReserved(code), false).Wait();

        private void Message()
        {
            if (_inMessage || _messageEventQueue.IsEmpty || _readyState != WebSocketState.Open)
                return;

            _inMessage = true;

            if (_messageEventQueue.TryDequeue(out var e))
                Messages(e);
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

            Task.Run(() => Messages(e));
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

            Messages(e);
        }

        private Task ProcessCloseFrame(WebSocketFrame frame) => InternalCloseAsync(frame.PayloadData, !frame.PayloadData.HasReservedCode, false);

        private async Task ProcessDataFrame(WebSocketFrame frame)
        {
            if (frame.IsCompressed)
            {
                var ms = await frame.PayloadData.ApplicationData.CompressAsync(_compression, System.IO.Compression.CompressionMode.Decompress).ConfigureAwait(false);

                _messageEventQueue.Enqueue(new MessageEventArgs(frame.Opcode, ms.ToArray()));
            }
            else
            {
                _messageEventQueue.Enqueue(new MessageEventArgs(frame));
            }
        }

        private async Task ProcessFragmentFrame(WebSocketFrame frame)
        {
            if (!InContinuation)
            {
                // Must process first fragment.
                if (frame.Opcode == Opcode.Cont)
                    return;

                _fragmentsBuffer = new FragmentBuffer(frame.Opcode, frame.IsCompressed);
                InContinuation = true;
            }

            _fragmentsBuffer.AddPayload(frame.PayloadData.ApplicationData);

            if (frame.Fin == Fin.Final)
            {
                using (_fragmentsBuffer)
                {
                    _messageEventQueue.Enqueue(await _fragmentsBuffer.GetMessage(_compression).ConfigureAwait(false));
                }

                _fragmentsBuffer = null;
                InContinuation = false;
            }
        }

        private Task ProcessPingFrame(WebSocketFrame frame)
        {
            if (EmitOnPing)
                _messageEventQueue.Enqueue(new MessageEventArgs(frame));

            return Send(new WebSocketFrame(Opcode.Pong, frame.PayloadData));
        }

        private void ProcessPongFrame()
        {
            _receivePong.Set();
            "Received a pong.".Trace(nameof(ProcessPongFrame));
        }

        private async Task<bool> ProcessReceivedFrame(WebSocketFrame frame)
        {
            if (frame.IsFragment)
            {
                await ProcessFragmentFrame(frame).ConfigureAwait(false);
            }
            else
            {
                switch (frame.Opcode)
                {
                    case Opcode.Text:
                    case Opcode.Binary:
                        await ProcessDataFrame(frame).ConfigureAwait(false);
                        break;
                    case Opcode.Ping:
                        await ProcessPingFrame(frame).ConfigureAwait(false);
                        break;
                    case Opcode.Pong:
                        ProcessPongFrame();
                        break;
                    case Opcode.Close:
                        await ProcessCloseFrame(frame).ConfigureAwait(false);
                        break;
                    default:
                        $"An unsupported frame: {frame.PrintToString()}".Error(nameof(ProcessReceivedFrame));
                        Fatal("There is no way to handle it.", CloseStatusCode.PolicyViolation);
                        return false;
                }
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

                if (comp || !ext.StartsWith(CompressionMethod.Deflate.ToExtensionString())) continue;

                _compression = CompressionMethod.Deflate;
                buff.AppendFormat(
                    "{0}, ",
                    _compression.ToExtensionString(
                        "client_no_context_takeover", "server_no_context_takeover"));

                comp = true;
            }

            var len = buff.Length;
            if (len > 2)
            {
                buff.Length = len - 2;
                _extensions = buff.ToString();
            }
        }

        private void ReleaseResources()
        {
            _context.CloseAsync();
            _stream = null;
            _context = null;

            if (_fragmentsBuffer != null)
            {
                _fragmentsBuffer.Dispose();
                _fragmentsBuffer = null;
                InContinuation = false;
            }

            if (_receivePong != null)
            {
                _receivePong.Dispose();
                _receivePong = null;
            }

            if (_exitReceiving == null) return;

            _exitReceiving.Dispose();
            _exitReceiving = null;
        }

        private Task Send(WebSocketFrame frame)
        {
            lock (_forState)
            {
                if (_readyState != WebSocketState.Open)
                {
                    "The sending has been interrupted.".Error(nameof(Send));
                    return Task.Delay(0);
                }
            }

            var frameAsBytes = frame.ToArray();
            return _stream.WriteAsync(frameAsBytes, 0, frameAsBytes.Length);
        }

        // As server
        private Task SendHandshakeAsync()
        {
            var ret = HttpResponse.CreateWebSocketResponse();

            var headers = ret.Headers;
            headers[HttpHeaderNames.SecWebSocketAccept] = WebSocketKey.CreateResponseKey();

            if (_extensions != null)
                headers[HttpHeaderNames.SecWebSocketExtensions] = _extensions;

            ret.SetCookies(CookieCollection);

            var bytes = Encoding.UTF8.GetBytes(ret.ToString());

            return _stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private void StartReceiving()
        {
            while (_messageEventQueue.TryDequeue(out _))
            {
                // do nothing
            }

            _exitReceiving = new AutoResetEvent(false);
            _receivePong = new AutoResetEvent(false);

            var frameStream = new WebSocketFrameStream(_stream);

            Task.Run(async () =>
            {
                while (_readyState == WebSocketState.Open)
                {
                    try
                    {
                        var frame = await frameStream.ReadFrameAsync(this).ConfigureAwait(false);

                        if (frame == null)
                            return;

                        var result = await ProcessReceivedFrame(frame).ConfigureAwait(false);

                        if (!result || _readyState == WebSocketState.Closed)
                        {
                            _exitReceiving?.Set();

                            return;
                        }

                        var _ = Task.Run(Message);
                    }
                    catch (Exception ex)
                    {
                        Fatal("An exception has occurred while receiving.", ex);
                    }
                }
            });
        }
    }
}

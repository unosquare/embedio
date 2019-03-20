namespace Unosquare.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
#if !NETSTANDARD1_3
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
#endif

    internal sealed class HttpConnection
    {
        internal const int BufferSize = 8192;

        private readonly Timer _timer;
        private readonly EndPointListener _epl;
        private Socket _sock;
        private MemoryStream _ms;
        private byte[] _buffer;
        private HttpListenerContext _context;
        private StringBuilder _currentLine;
        private RequestStream _iStream;
        private ResponseStream _oStream;
        private bool _contextBound;
        private int _sTimeout = 90000; // 90k ms for first request, 15k ms from then on        
        private IPEndPoint _localEp;
        private HttpListener _lastListener;
        private InputState _inputState = InputState.RequestLine;
        private LineState _lineState = LineState.None;
        private int _position;

#if !NETSTANDARD1_3
        public HttpConnection(Socket sock, EndPointListener epl, X509Certificate cert)
#else
        public HttpConnection(Socket sock, EndPointListener epl)
#endif
        {
            _sock = sock;
            _epl = epl;
            IsSecure = epl.Secure;

#if !NETSTANDARD1_3

            if (!IsSecure)
            {
                Stream = new NetworkStream(sock, false);
            }
            else
            {
                var sslStream = new SslStream(new NetworkStream(sock, false), true);
                sslStream.AuthenticateAsServerAsync(cert).GetAwaiter().GetResult();

                Stream = sslStream;
            }
#else
            Stream = new NetworkStream(sock, false);
#endif
            _timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            Init();
        }

#if !NETSTANDARD1_3
        internal X509Certificate2 ClientCertificate { get; }
#endif

        public int Reuses { get; private set; }

        public Stream Stream { get; }

        public IPEndPoint LocalEndPoint => _localEp ?? (_localEp = (IPEndPoint)_sock.LocalEndPoint);

        public IPEndPoint RemoteEndPoint => (IPEndPoint)_sock?.RemoteEndPoint;

        public bool IsSecure { get; }

        public ListenerPrefix Prefix { get; set; }

        public async Task BeginReadRequest()
        {
            if (_buffer == null)
                _buffer = new byte[BufferSize];

            try
            {
                if (Reuses == 1)
                    _sTimeout = 15000;
                _timer.Change(_sTimeout, Timeout.Infinite);

                var data = await Stream.ReadAsync(_buffer, 0, BufferSize).ConfigureAwait(false);
                await OnReadInternal(data).ConfigureAwait(false);
            }
            catch
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                CloseSocket();
                Unbind();
            }
        }

        public RequestStream GetRequestStream(long contentLength)
        {
            if (_iStream != null) return _iStream;

            var buffer = _ms.ToArray();
            var length = (int)_ms.Length;
            _ms = null;

            _iStream = new RequestStream(Stream, buffer, _position, length - _position, contentLength);

            return _iStream;
        }

        public ResponseStream GetResponseStream() => _oStream ??
                                                     (_oStream =
                                                         new ResponseStream(Stream, _context.HttpListenerResponse, _context.Listener?.IgnoreWriteExceptions ?? true));

        internal void Close(bool forceClose = false)
        {
            if (_sock != null)
            {
                GetResponseStream()?.Dispose();

                _oStream = null;
            }

            if (_sock == null) return;

            forceClose |= !_context.Request.KeepAlive;

            if (!forceClose)
                forceClose = _context.Response.Headers["connection"] == "close";

            if (!forceClose)
            {
                if (_context.HttpListenerRequest.FlushInput().GetAwaiter().GetResult())
                {
                    Reuses++;
                    Unbind();
                    Init();
#pragma warning disable 4014
                    BeginReadRequest();
#pragma warning restore 4014
                    return;
                }
            }

            var s = _sock;
            _sock = null;

            try
            {
                s?.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // ignored
            }
            finally
            {
                s?.Dispose();
            }

            Unbind();
            RemoveConnection();
        }

        private void Init()
        {
            _contextBound = false;
            _iStream = null;
            _oStream = null;
            Prefix = null;
            _ms = new MemoryStream();
            _position = 0;
            _inputState = InputState.RequestLine;
            _lineState = LineState.None;
            _context = new HttpListenerContext(this);
        }

        private void OnTimeout(object unused)
        {
            CloseSocket();
            Unbind();
        }

        private async Task OnReadInternal(int offset)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            // Continue reading until full header is received.
            // Especially important for multipart requests when the second part of the header arrives after a tiny delay
            // because the web browser has to measure the content length first.
            var parsedBytes = 0;
            while (true)
            {
                try
                {
                    await _ms.WriteAsync(_buffer, parsedBytes, offset - parsedBytes).ConfigureAwait(false);
                    if (_ms.Length > 32768)
                    {
                        Close(true);
                        return;
                    }
                }
                catch
                {
                    CloseSocket();
                    Unbind();
                    return;
                }

                if (offset == 0)
                {
                    CloseSocket();
                    Unbind();
                    return;
                }

                if (ProcessInput(_ms))
                {
                    if (!_context.HaveError)
                        _context.HttpListenerRequest.FinishInitialization();

                    if (_context.HaveError || !_epl.BindContext(_context))
                    {
                        Close(true);
                        return;
                    }

                    var listener = _context.Listener;
                    if (_lastListener != listener)
                    {
                        RemoveConnection();
                        listener.AddConnection(this);
                        _lastListener = listener;
                    }

                    _contextBound = true;
                    listener.RegisterContext(_context);
                    return;
                }

                parsedBytes = offset;
                offset += await Stream.ReadAsync(_buffer, offset, BufferSize - offset).ConfigureAwait(false);
            }
        }

        private void RemoveConnection()
        {
            if (_lastListener == null)
                _epl.RemoveConnection(this);
            else
                _lastListener.RemoveConnection(this);
        }

        // true -> done processing
        // false -> need more input
        private bool ProcessInput(MemoryStream ms)
        {
            var buffer = ms.ToArray();
            var len = (int)ms.Length;
            var used = 0;

            while (true)
            {
                if (_context.HaveError)
                    return true;

                if (_position >= len)
                    break;

                string line;
                try
                {
                    line = ReadLine(buffer, _position, len - _position, out used);
                    _position += used;
                }
                catch
                {
                    _context.ErrorMessage = "Bad request";
                    return true;
                }

                if (line == null)
                    break;

                if (string.IsNullOrEmpty(line))
                {
                    if (_inputState == InputState.RequestLine)
                        continue;
                    _currentLine = null;

                    return true;
                }

                if (_inputState == InputState.RequestLine)
                {
                    _context.HttpListenerRequest.SetRequestLine(line);
                    _inputState = InputState.Headers;
                }
                else
                {
                    try
                    {
                        _context.HttpListenerRequest.AddHeader(line);
                    }
                    catch (Exception e)
                    {
                        _context.ErrorMessage = e.Message;
                        return true;
                    }
                }
            }

            if (used == len)
            {
                ms.SetLength(0);
                _position = 0;
            }

            return false;
        }

        private string ReadLine(byte[] buffer, int offset, int len, out int used)
        {
            if (_currentLine == null)
                _currentLine = new StringBuilder(128);

            var last = offset + len;
            used = 0;
            for (var i = offset; i < last && _lineState != LineState.Lf; i++)
            {
                used++;
                var b = buffer[i];

                switch (b)
                {
                    case 13:
                        _lineState = LineState.Cr;
                        break;
                    case 10:
                        _lineState = LineState.Lf;
                        break;
                    default:
                        _currentLine.Append((char)b);
                        break;
                }
            }

            if (_lineState != LineState.Lf) return null;
            _lineState = LineState.None;
            var result = _currentLine.ToString();
            _currentLine.Length = 0;

            return result;
        }

        private void Unbind()
        {
            if (!_contextBound) return;

            _epl.UnbindContext(_context);
            _contextBound = false;
        }

        private void CloseSocket()
        {
            if (_sock == null)
                return;

            try
            {
                _sock.Dispose();
            }
            finally
            {
                _sock = null;
            }

            RemoveConnection();
        }

        private enum InputState
        {
            RequestLine,
            Headers,
        }

        private enum LineState
        {
            None,
            Cr,
            Lf,
        }
    }
}
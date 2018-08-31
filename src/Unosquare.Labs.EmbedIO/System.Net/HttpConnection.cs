namespace Unosquare.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
#if SSL
using System.Security.Cryptography.X509Certificates;
#endif

    internal sealed class HttpConnection
    {
        private enum InputState
        {
            RequestLine,
            Headers
        }

        private enum LineState
        {
            None,
            Cr,
            Lf
        }

        private const int BufferSize = 8192;
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

#if SSL
        private X509Certificate _cert;
        IMonoSslStream ssl_stream;
#endif

#if SSL
        public HttpConnection(Socket sock, EndPointListener epl, bool secure, X509Certificate cert)
#else
        public HttpConnection(Socket sock, EndPointListener epl)
#endif
        {
            _sock = sock;
            _epl = epl;

#if SSL
            IsSecure = secure;

            if (!secure)
            {
                Stream = new NetworkStream(sock, false);
            }
            else
            {                
            _cert = cert;

                ssl_stream = epl.Listener.CreateSslStream(new NetworkStream(sock, false), false, (t, c, ch, e) =>
                {
                    if (c == null)
                        return true;
                    var c2 = c as X509Certificate2;
                    if (c2 == null)
                        c2 = new X509Certificate2(c.GetRawCertData());
                    client_cert = c2;
                    client_cert_errors = new int[] { (int)e };
                    return true;
                });
                stream = ssl_stream.AuthenticatedStream;
            }
#else
            Stream = new NetworkStream(sock, false);
#endif
            _timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            Init();
        }

#if SSL
        internal int[] ClientCertificateErrors { get; }

        internal X509Certificate2 ClientCertificate { get; }
#endif

        public bool IsClosed => _sock == null;

        public int Reuses { get; private set; }

        public Stream Stream { get; }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (_localEp != null)
                    return _localEp;

                _localEp = (IPEndPoint)_sock.LocalEndPoint;
                return _localEp;
            }
        }

        public IPEndPoint RemoteEndPoint => (IPEndPoint)_sock?.RemoteEndPoint;
#if SSL
        public bool IsSecure { get; }
#endif
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

                var data = await Stream.ReadAsync(_buffer, 0, BufferSize);
                await OnReadInternal(data);
            }
            catch
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                CloseSocket();
                Unbind();
            }
        }

        public RequestStream GetRequestStream(long contentlength)
        {
            if (_iStream != null) return _iStream;

            var buffer = _ms.ToArray();
            var length = (int) _ms.Length;
            _ms = null;

            _iStream = new RequestStream(Stream, buffer, _position, length - _position, contentlength);

            return _iStream;
        }

        public ResponseStream GetResponseStream()
        {
            return _oStream ??
                   (_oStream =
                       new ResponseStream(Stream, _context.HttpListenerResponse, _context.Listener?.IgnoreWriteExceptions ?? true));
        }

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
#if SSL
            if (ssl_stream != null)
            {
                ssl_stream.AuthenticateAsServer(cert, true, (SslProtocols)ServicePointManager.SecurityProtocol, false);
            }
#endif
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

        private async Task OnReadInternal(int nread)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            // Continue reading until full header is received.
            // Especially important for multipart requests when the second part of the header arrives after a tiny delay
            // because the webbrowser has to meassure the content length first.
            var parsedBytes = 0;
            while (true)
            {
                try
                {
                    await _ms.WriteAsync(_buffer, parsedBytes, nread - parsedBytes);
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

                if (nread == 0)
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

                parsedBytes = nread;
                nread += await Stream.ReadAsync(_buffer, nread, BufferSize - nread);
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
                    line = ReadLine(buffer, _position, len - _position, ref used);
                    _position += used;
                }
                catch
                {
                    _context.ErrorMessage = "Bad request";
                    _context.ErrorStatus = 400;
                    return true;
                }

                if (line == null)
                    break;

                if (line == string.Empty)
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
                        _context.ErrorStatus = 400;
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

        private string ReadLine(byte[] buffer, int offset, int len, ref int used)
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
            if (_contextBound)
            {
                _epl.UnbindContext(_context);
                _contextBound = false;
            }
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
    }
}
#if !NET452
//
// System.Net.HttpConnection
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo.mono@gmail.com)
//
// Copyright (c) 2005-2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2012 Xamarin, Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace System.Net
{
    internal sealed class HttpConnection
    {
        private static readonly AsyncCallback _onreadCb = OnRead;
        private const int BufferSize = 8192;
        private Socket _sock;
        private readonly Stream _stream;
        private readonly EndPointListener _epl;
        private MemoryStream _ms;
        private byte[] _buffer;
        private HttpListenerContext _context;
        private StringBuilder _currentLine;
        private RequestStream _iStream;
        private ResponseStream _oStream;
        private bool _chunked;
        private bool _contextBound;
        private X509Certificate _cert;
        private int _sTimeout = 90000; // 90k ms for first request, 15k ms from then on
        private readonly Timer _timer;
        private IPEndPoint _localEp;
        private HttpListener _lastListener;
#if SSL
        IMonoSslStream ssl_stream;
#endif

        public HttpConnection(Socket sock, EndPointListener epl, bool secure, X509Certificate cert)
        {
            _sock = sock;
            _epl = epl;
            IsSecure = secure;
            _cert = cert;
            if (secure == false)
            {
                _stream = new NetworkStream(sock, false);
            }
            else
            {
#if SSL
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
#else
                throw new Exception("SSL is not supported");
#endif
            }
            _timer = new Timer(OnTimeout, null, Timeout.Infinite, Timeout.Infinite);
            Init();
        }

        internal int[] ClientCertificateErrors { get; }

        internal X509Certificate2 ClientCertificate { get; }

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
            _chunked = false;
            _ms = new MemoryStream();
            _position = 0;
            _inputState = InputState.RequestLine;
            _lineState = LineState.None;
            _context = new HttpListenerContext(this);
        }

        public bool IsClosed => (_sock == null);

        public int Reuses { get; private set; }

        public IPEndPoint LocalEndPoint
        {
            get
            {
                if (_localEp != null)
                    return _localEp;

                _localEp = (IPEndPoint) _sock.LocalEndPoint;
                return _localEp;
            }
        }

        public IPEndPoint RemoteEndPoint => (IPEndPoint) _sock.RemoteEndPoint;

        public bool IsSecure { get; }

        public ListenerPrefix Prefix { get; set; }

        private void OnTimeout(object unused)
        {
            CloseSocket();
            Unbind();
        }

        public void BeginReadRequest()
        {
            if (_buffer == null)
                _buffer = new byte[BufferSize];
            try
            {
                if (Reuses == 1)
                    _sTimeout = 15000;
                _timer.Change(_sTimeout, Timeout.Infinite);
                _stream.BeginRead(_buffer, 0, BufferSize, _onreadCb, this);
            }
            catch
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                CloseSocket();
                Unbind();
            }
        }

        public RequestStream GetRequestStream(bool chunked, long contentlength)
        {
            if (_iStream == null)
            {
                var buffer = _ms.GetBuffer();
                var length = (int) _ms.Length;
                _ms = null;
                if (chunked)
                {
                    _chunked = true;
                    _context.Response.SendChunked = true;
                    _iStream = new ChunkedInputStream(_context, _stream, buffer, _position, length - _position);
                }
                else
                {
                    _iStream = new RequestStream(_stream, buffer, _position, length - _position, contentlength);
                }
            }
            return _iStream;
        }

        public ResponseStream GetResponseStream()
        {
            // TODO: can we get this stream before reading the input?
            if (_oStream == null)
            {
                var listener = _context.Listener;

                if (listener == null)
                    return new ResponseStream(_stream, _context.Response, true);

                _oStream = new ResponseStream(_stream, _context.Response, listener.IgnoreWriteExceptions);
            }
            return _oStream;
        }

        private static void OnRead(IAsyncResult ares)
        {
            var cnc = (HttpConnection) ares.AsyncState;
            cnc.OnReadInternal(ares);
        }

        private void OnReadInternal(IAsyncResult ares)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            var nread = -1;
            try
            {
                nread = _stream.EndRead(ares);
                _ms.Write(_buffer, 0, nread);
                if (_ms.Length > 32768)
                {
                    SendError("Bad request", 400);
                    Close(true);
                    return;
                }
            }
            catch
            {
                if (_ms != null && _ms.Length > 0)
                    SendError();
                if (_sock != null)
                {
                    CloseSocket();
                    Unbind();
                }
                return;
            }

            if (nread == 0)
            {
                //if (ms.Length > 0)
                //	SendError (); // Why bother?
                CloseSocket();
                Unbind();
                return;
            }

            if (ProcessInput(_ms))
            {
                if (!_context.HaveError)
                    _context.Request.FinishInitialization();

                if (_context.HaveError)
                {
                    SendError();
                    Close(true);
                    return;
                }

                if (!_epl.BindContext(_context))
                {
                    SendError("Invalid host", 400);
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
            _stream.BeginRead(_buffer, 0, BufferSize, _onreadCb, this);
        }

        private void RemoveConnection()
        {
            if (_lastListener == null)
                _epl.RemoveConnection(this);
            else
                _lastListener.RemoveConnection(this);
        }

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

        private InputState _inputState = InputState.RequestLine;
        private LineState _lineState = LineState.None;
        private int _position;

        // true -> done processing
        // false -> need more input
        private bool ProcessInput(MemoryStream ms)
        {
            var buffer = ms.GetBuffer();
            var len = (int) ms.Length;
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

                if (line == "")
                {
                    if (_inputState == InputState.RequestLine)
                        continue;
                    _currentLine = null;
                    ms = null;
                    return true;
                }

                if (_inputState == InputState.RequestLine)
                {
                    _context.Request.SetRequestLine(line);
                    _inputState = InputState.Headers;
                }
                else
                {
                    try
                    {
                        _context.Request.AddHeader(line);
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
                if (b == 13)
                {
                    _lineState = LineState.Cr;
                }
                else if (b == 10)
                {
                    _lineState = LineState.Lf;
                }
                else
                {
                    _currentLine.Append((char) b);
                }
            }

            string result = null;
            if (_lineState == LineState.Lf)
            {
                _lineState = LineState.None;
                result = _currentLine.ToString();
                _currentLine.Length = 0;
            }

            return result;
        }

        public void SendError(string msg, int status)
        {
            try
            {
                var response = _context.Response;
                response.StatusCode = status;
                response.ContentType = "text/html";
                var description = HttpListenerResponseHelper.GetStatusDescription(status);
                var str = msg != null
                    ? string.Format("<h1>{0} ({1})</h1>", description, msg)
                    : string.Format("<h1>{0}</h1>", description);

                var error = _context.Response.ContentEncoding.GetBytes(str);
                response.Close(error, false);
            }
            catch
            {
                // response was already closed
            }
        }

        public void SendError()
        {
            SendError(_context.ErrorMessage, _context.ErrorStatus);
        }

        private void Unbind()
        {
            if (_contextBound)
            {
                _epl.UnbindContext(_context);
                _contextBound = false;
            }
        }

        public void Close()
        {
            Close(false);
        }

        private void CloseSocket()
        {
            if (_sock == null)
                return;

            try
            {
                _sock.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                _sock = null;
            }
            RemoveConnection();
        }

        internal void Close(bool forceClose)
        {
            if (_sock != null)
            {
                Stream st = GetResponseStream();
                st?.Dispose();

                _oStream = null;
            }

            if (_sock != null)
            {
                forceClose |= !_context.Request.KeepAlive;
                if (!forceClose)
                    forceClose = (_context.Response.Headers["connection"] == "close");
                /*
				if (!force_close) {
//					bool conn_close = (status_code == 400 || status_code == 408 || status_code == 411 ||
//							status_code == 413 || status_code == 414 || status_code == 500 ||
//							status_code == 503);
					force_close |= (context.Request.ProtocolVersion <= HttpVersion.Version10);
				}
				*/

                if (!forceClose && _context.Request.FlushInput())
                {
                    if (_chunked && _context.Response.ForceCloseChunked == false)
                    {
                        // Don't close. Keep working.
                        Reuses++;
                        Unbind();
                        Init();
                        BeginReadRequest();
                        return;
                    }

                    Reuses++;
                    Unbind();
                    Init();
                    BeginReadRequest();
                    return;
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
        }
    }
}

#endif
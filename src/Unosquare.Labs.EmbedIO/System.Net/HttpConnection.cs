#if !NET46
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

namespace Unosquare.Net
{
    internal sealed class HttpConnection
    {
        private const int BufferSize = 8192;
        private Socket _sock;
        private readonly EndPointListener _epl;
        private MemoryStream _ms;
        private byte[] _buffer;
        private HttpListenerContext _context;
        private StringBuilder _currentLine;
        private RequestStream _iStream;
        private ResponseStream _oStream;
        private bool _chunked;
        private bool _contextBound;
        private int _sTimeout = 90000; // 90k ms for first request, 15k ms from then on
        private readonly Timer _timer;
        private IPEndPoint _localEp;
        private HttpListener _lastListener;
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

            if (secure == false)
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

        public Stream Stream { get; }

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

        public IPEndPoint RemoteEndPoint => (IPEndPoint) _sock?.RemoteEndPoint;

        public bool IsSecure { get; }

        public ListenerPrefix Prefix { get; set; }

        private void OnTimeout(object unused)
        {
            CloseSocket();
            Unbind();
        }

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

        public RequestStream GetRequestStream(bool chunked, long contentlength)
        {
            if (_iStream != null) return _iStream;

            var buffer = _ms.ToArray();
            var length = (int) _ms.Length;
            _ms = null;

            if (chunked)
            {
#if CHUNKED
                _chunked = true;
                _context.Response.SendChunked = true;
                _iStream = new ChunkedInputStream(_context, Stream, buffer, _position, length - _position);
#else
                throw new InvalidOperationException("Chunked transfer encoding is not supported");
#endif
            }
            else
            {
                _iStream = new RequestStream(Stream, buffer, _position, length - _position, contentlength);
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
                    return new ResponseStream(Stream, _context.Response, true);

                _oStream = new ResponseStream(Stream, _context.Response, listener.IgnoreWriteExceptions);
            }

            return _oStream;
        }

        private async Task OnReadInternal(int nread)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            // Continue reading until full header is received.
            // Especially important for multipart requests when the second part of the header arrives after a tiny delay
            // because the webbrowser has to meassure the content length first.
            int parsedBytes = 0;
            while (true)
            {
                try
                {
                    await _ms.WriteAsync(_buffer, parsedBytes, nread - parsedBytes);
                    if (_ms.Length > 32768)
                    {
                        await CloseAsync(true);
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

                    if (_context.HaveError || !_epl.BindContext(_context))
                    {
                        await CloseAsync(true);
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
            var buffer = ms.ToArray();
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

                if (line == string.Empty)
                {
                    if (_inputState == InputState.RequestLine)
                        continue;
                    _currentLine = null;

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

                switch (b)
                {
                    case 13:
                        _lineState = LineState.Cr;
                        break;
                    case 10:
                        _lineState = LineState.Lf;
                        break;
                    default:
                        _currentLine.Append((char) b);
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

        internal async Task CloseAsync(bool forceClose = false)
        {
            if (_sock != null)
            {
                Stream st = GetResponseStream();
                st?.Dispose();

                _oStream = null;
            }

            if (_sock == null) return;

            forceClose |= !_context.Request.KeepAlive;

            if (!forceClose)
                forceClose = (_context.Response.Headers["connection"] == "close");

            if (!forceClose)
            {
                var isValidInput = await _context.Request.FlushInput();

                if (isValidInput)
                {
                    if (_chunked && _context.Response.ForceCloseChunked == false)
                    {
                        // Don't close. Keep working.
                        Reuses++;
                        Unbind();
                        Init();
                        await BeginReadRequest().ConfigureAwait(false);
                        return;
                    }

                    Reuses++;
                    Unbind();
                    Init();
                    await BeginReadRequest().ConfigureAwait(false);
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
    }
}

#endif
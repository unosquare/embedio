namespace Unosquare.Net
{
    using System;
    using System.Globalization;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.IO;
    using System.Text;
    using Labs.EmbedIO;

    /// <summary>
    /// Represents an HTTP Listener's response.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class HttpListenerResponse 
        : IHttpResponse, IDisposable
    {
        private const string CannotChangeHeaderWarning = "Cannot be changed after headers are sent.";
        private readonly HttpListenerContext _context;
        private bool _disposed;
        private long _contentLength;
        private bool _clSet;
        private string _contentType;
        private CookieCollection _cookies;
        private bool _keepAlive = true;
        private ResponseStream _outputStream;
        private int _statusCode = 200;
        private bool _chunked;

        internal HttpListenerResponse(HttpListenerContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Encoding ContentEncoding { get; set; } = Encoding.UTF8;

        /// <inheritdoc />
        public long ContentLength64
        {
            get => _contentLength;

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Must be >= 0");

                _clSet = true;
                _contentLength = value;
            }
        }

        /// <inheritdoc />
        public string ContentType
        {
            get => _contentType;

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                _contentType = value;
            }
        }

        /// <inheritdoc />
        public ICookieCollection Cookies => CookieCollection;

        /// <inheritdoc />
        public NameValueCollection Headers => HeaderCollection;
        
        /// <inheritdoc />
        public bool KeepAlive
        {
            get => _keepAlive;

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                _keepAlive = value;
            }
        }

        /// <inheritdoc />
        public Stream OutputStream =>
            _outputStream ?? (_outputStream = _context.Connection.GetResponseStream());

        /// <inheritdoc />
        public Version ProtocolVersion { get; set; } = HttpVersion.Version11;

        /// <summary>
        /// Gets or sets a value indicating whether [send chunked].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [send chunked]; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the 
        /// IDisposable interface, and that object has been disposed.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot be changed after headers are sent.</exception>
        public bool SendChunked
        {
            get => _chunked;

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                _chunked = value;
            }
        }

        /// <inheritdoc />
        public int StatusCode
        {
            get => _statusCode;

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                if (value < 100 || value > 999)
                    throw new ProtocolViolationException("StatusCode must be between 100 and 999.");
                _statusCode = value;
                StatusDescription = HttpListenerResponseHelper.GetStatusDescription(value);
            }
        }

        /// <summary>
        /// Gets or sets the status description.
        /// </summary>
        /// <value>
        /// The status description.
        /// </value>
        public string StatusDescription { get; set; } = "OK";
        
        internal CookieCollection CookieCollection
        {
            get => _cookies ?? (_cookies = new CookieCollection());
            set => _cookies = value;
        }

        internal WebHeaderCollection HeaderCollection { get; set; } = new WebHeaderCollection();

        internal bool HeadersSent { get; private set; }
        internal object HeadersLock { get; } = new object();
        internal bool ForceCloseChunked { get; private set; }

        void IDisposable.Dispose() => Close(true);

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        public void Abort()
        {
            if (_disposed == false)
                return;

            Close(true);
        }

        /// <inheritdoc />
        public void AddHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("'name' cannot be empty", nameof(name));

            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value));

            Headers[name] = value;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            if (_disposed)
                return;

            Close(false);
        }

        /// <inheritdoc />
        public void SetCookie(Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException(nameof(cookie));

            if (_cookies != null)
            {
                if (_cookies.Any(c =>
                    cookie.Name == c.Name && cookie.Domain == c.Domain && cookie.Path == c.Path))
                    throw new ArgumentException("The cookie already exists.");
            }
            else
            {
                _cookies = new CookieCollection();
            }

            _cookies.Add(cookie);
        }

        internal void SendHeaders(bool closing, MemoryStream ms)
        {
            if (_contentType != null)
            {
                if (_contentType.IndexOf("charset=", StringComparison.Ordinal) == -1)
                {
                    HeaderCollection.AddWithoutValidate("Content-Type", _contentType + "; charset=" + Encoding.UTF8.WebName);
                }
                else
                {
                    HeaderCollection.AddWithoutValidate("Content-Type", _contentType);
                }
            }

            if (Headers["Server"] == null)
                HeaderCollection.AddWithoutValidate("Server", "embedio/2.0");

            var inv = CultureInfo.InvariantCulture;
            if (Headers["Date"] == null)
                HeaderCollection.AddWithoutValidate("Date", DateTime.UtcNow.ToString("r", inv));

            if (!_chunked)
            {
                if (!_clSet && closing)
                {
                    _clSet = true;
                    _contentLength = 0;
                }

                if (_clSet)
                    HeaderCollection.AddWithoutValidate("Content-Length", _contentLength.ToString(inv));
            }

            var v = _context.Request.ProtocolVersion;
            if (!_clSet && !_chunked && v >= HttpVersion.Version11)
                _chunked = true;

            //// Apache forces closing the connection for these status codes:
            //// HttpStatusCode.BadRequest        400
            //// HttpStatusCode.RequestTimeout        408
            //// HttpStatusCode.LengthRequired        411
            //// HttpStatusCode.RequestEntityTooLarge     413
            //// HttpStatusCode.RequestUriTooLong     414
            //// HttpStatusCode.InternalServerError   500
            //// HttpStatusCode.ServiceUnavailable    503        
            var connClose = _statusCode == 400 || _statusCode == 408 || _statusCode == 411 ||
                            _statusCode == 413 || _statusCode == 414 || _statusCode == 500 ||
                            _statusCode == 503;

            if (connClose == false)
                connClose = !_context.Request.KeepAlive;

            // They sent both KeepAlive: true and Connection: close!?
            if (!_keepAlive || connClose)
            {
                HeaderCollection.AddWithoutValidate("Connection", "close");
                connClose = true;
            }

            if (_chunked)
                HeaderCollection.AddWithoutValidate("Transfer-Encoding", "chunked");

            var reuses = _context.Connection.Reuses;
            if (reuses >= 100)
            {
                ForceCloseChunked = true;
                if (!connClose)
                {
                    HeaderCollection.AddWithoutValidate("Connection", "close");
                    connClose = true;
                }
            }

            if (!connClose)
            {
                HeaderCollection.AddWithoutValidate("Keep-Alive", $"timeout=15,max={100 - reuses}");
                if (_context.Request.ProtocolVersion <= HttpVersion.Version10)
                    HeaderCollection.AddWithoutValidate("Connection", "keep-alive");
            }

            if (_cookies != null)
            {
                foreach (var cookie in _cookies)
                    HeaderCollection.AddWithoutValidate("Set-Cookie", CookieToClientString(cookie));
            }

            WriteHeaders(ms);
        }

        private static string FormatHeaders(NameValueCollection headers)
        {
            var sb = new StringBuilder();

            foreach (var key in headers.AllKeys)
                sb.Append(key).Append(": ").Append(headers[key]).Append("\r\n");

            return sb.Append("\r\n").ToString();
        }

        private static string CookieToClientString(Cookie cookie)
        {
            if (cookie.Name.Length == 0)
                return string.Empty;

            var result = new StringBuilder(64);

            if (cookie.Version > 0)
                result.Append("Version=").Append(cookie.Version).Append(";");

            result.Append(cookie.Name).Append("=").Append(cookie.Value);

            if (!string.IsNullOrEmpty(cookie.Path))
                result.Append(";Path=").Append(QuotedString(cookie, cookie.Path));

            if (!string.IsNullOrEmpty(cookie.Domain))
                result.Append(";Domain=").Append(QuotedString(cookie, cookie.Domain));

            if (!string.IsNullOrEmpty(cookie.Port))
                result.Append(";Port=").Append(cookie.Port);

            return result.ToString();
        }

        private static string QuotedString(Cookie cookie, string value)
            => cookie.Version == 0 || value.IsToken() ? value : "\"" + value.Replace("\"", "\\\"") + "\"";

        private void Close(bool force)
        {
            _disposed = true;

            _context.Connection.Close(force);
        }

        private void WriteHeaders(Stream ms)
        {
            using (var writer = new StreamWriter(ms, Encoding.UTF8, 256))
            {
                writer.Write("HTTP/{0} {1} {2}\r\n", ProtocolVersion, _statusCode, StatusDescription);
                var headersStr = FormatHeaders(HeaderCollection);
                writer.Write(headersStr);
                writer.Flush();

                var preamble = Encoding.UTF8.GetPreamble().Length;
                if (_outputStream == null)
                    _outputStream = _context.Connection.GetResponseStream();

                // Assumes that the ms was at position 0
                ms.Position = preamble;
                HeadersSent = true;
            }
        }
    }
}
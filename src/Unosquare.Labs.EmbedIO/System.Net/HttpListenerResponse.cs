namespace Unosquare.Net
{
    using Labs.EmbedIO;
    using System;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Represents an HTTP Listener's response.
    /// </summary>
    /// <seealso cref="IDisposable" />
    internal sealed class HttpListenerResponse
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
        /// <exception cref="ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the 
        /// IDisposable interface, and that object has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">Cannot be changed after headers are sent.</exception>
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void AddHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("'name' cannot be empty", nameof(name));

            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value));

            Headers[name] = value;
        }

        public void Close()
        {
            if (!_disposed) Close(false);
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

        internal MemoryStream SendHeaders(bool closing)
        {
            if (_contentType != null)
            {
                var contentTypeValue = _contentType.IndexOf("charset=", StringComparison.Ordinal) == -1
                    ? $"{_contentType}; charset={Encoding.UTF8.WebName}"
                    : _contentType;

                HeaderCollection.Add("Content-Type", contentTypeValue);
            }

            if (Headers["Server"] == null)
                HeaderCollection.Add("Server", HttpResponse.ServerVersion);

            var inv = CultureInfo.InvariantCulture;
            if (Headers["Date"] == null)
                HeaderCollection.Add("Date", DateTime.UtcNow.ToString("r", inv));

            if (!_chunked)
            {
                if (!_clSet && closing)
                {
                    _clSet = true;
                    _contentLength = 0;
                }

                if (_clSet)
                    HeaderCollection.Add("Content-Length", _contentLength.ToString(inv));
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
                HeaderCollection.Add("Connection", "close");
                connClose = true;
            }

            if (_chunked)
                HeaderCollection.Add("Transfer-Encoding", "chunked");

            var reuses = _context.Connection.Reuses;
            if (reuses >= 100)
            {
                ForceCloseChunked = true;
                if (!connClose)
                {
                    HeaderCollection.Add("Connection", "close");
                    connClose = true;
                }
            }

            if (!connClose)
            {
                HeaderCollection.Add("Keep-Alive", $"timeout=15,max={100 - reuses}");

                if (_context.Request.ProtocolVersion <= HttpVersion.Version10)
                    HeaderCollection.Add("Connection", "keep-alive");
            }

            if (_cookies != null)
            {
                foreach (var cookie in _cookies)
                    HeaderCollection.Add("Set-Cookie", CookieToClientString(cookie));
            }

            return WriteHeaders();
        }

        private static string CookieToClientString(Cookie cookie)
        {
            if (cookie.Name.Length == 0)
                return string.Empty;

            var result = new StringBuilder(64);

            if (cookie.Version > 0)
                result.Append("Version=").Append(cookie.Version).Append("; ");

            result
                .Append(cookie.Name)
                .Append("=")
                .Append(cookie.Value);
            
            if (cookie.Expires != DateTime.MinValue)
            {
                result
                    .Append("; Expires=")
                    .Append(cookie.Expires.ToUniversalTime().ToString("ddd, dd-MMM-yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo))
                    .Append(" GMT");
            }

            if (!string.IsNullOrEmpty(cookie.Path))
                result.Append("; Path=").Append(QuotedString(cookie, cookie.Path));

            if (!string.IsNullOrEmpty(cookie.Domain))
                result.Append("; Domain=").Append(QuotedString(cookie, cookie.Domain));

            if (!string.IsNullOrEmpty(cookie.Port))
                result.Append("; Port=").Append(cookie.Port);
            
            if (cookie.Secure)
                result.Append("; Secure");
            
            if (cookie.HttpOnly)
                result.Append("; HttpOnly");

            return result.ToString();
        }

        private static string QuotedString(Cookie cookie, string value)
            => cookie.Version == 0 || value.IsToken() ? value : "\"" + value.Replace("\"", "\\\"") + "\"";
        
        private void Close(bool force)
        {
            _disposed = true;

            _context.Connection.Close(force);
        }

        private string GetHeaderData()
        {
            var sb = new StringBuilder()
                .AppendFormat("HTTP/{0} {1} {2}\r\n", ProtocolVersion, _statusCode, StatusDescription);

            foreach (var key in HeaderCollection.AllKeys.Where(x => x != "Set-Cookie"))
                sb.AppendFormat("{0}: {1}\r\n", key, HeaderCollection[key]);

            foreach (var cookie in HeaderCollection.GetCookies(true))
                sb.AppendFormat("Set-Cookie: {0}\r\n", CookieToClientString(cookie));

            return sb.Append("\r\n").ToString();
        }

        private MemoryStream WriteHeaders()
        {
            var stream = new MemoryStream();
            var data = Encoding.UTF8.GetBytes(GetHeaderData());
            var preamble = Encoding.UTF8.GetPreamble();
            stream.Write(preamble, 0, preamble.Length);
            stream.Write(data, 0, data.Length);

            if (_outputStream == null)
                _outputStream = _context.Connection.GetResponseStream();

            // Assumes that the ms was at position 0
            stream.Position = preamble.Length;
            HeadersSent = true;

            return stream;
        }
    }
}
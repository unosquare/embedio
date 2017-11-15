#if !NET47
//
// System.Net.HttpListenerResponse
//
// Author:
// Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
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
namespace Unosquare.Net
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Represents an HTTP Listener's response
    /// </summary>
    /// <seealso cref="IDisposable" />
    public sealed class HttpListenerResponse : IDisposable
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
        private Version _version = HttpVersion.Version11;
        private int _statusCode = 200;
        private bool _chunked;

        internal HttpListenerResponse(HttpListenerContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets or sets the content encoding.
        /// </summary>
        public Encoding ContentEncoding => Encoding.UTF8;

        /// <summary>
        /// Gets or sets the content length.
        /// </summary>
        /// <value>
        /// The content length64.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the 
        /// IDisposable interface, and that object has been disposed
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot be changed after headers are sent.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Must be >= 0 - value</exception>
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

        /// <summary>
        /// Gets or sets the MIME type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the IDisposable 
        /// interface, and that object has been disposed
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot be changed after headers are sent.</exception>
        public string ContentType
        {
            get => _contentType;

            set
            {
                // TODO: is null ok?
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                _contentType = value;
            }
        }

        // RFC 2109, 2965 + the netscape specification at http://wp.netscape.com/newsref/std/cookie_spec.html

        /// <summary>
        /// Gets or sets the cookies collection.
        /// </summary>
        /// <value>
        /// The cookies.
        /// </value>
        public CookieCollection Cookies
        {
            get => _cookies ?? (_cookies = new CookieCollection());
            set => _cookies = value;
        }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public WebHeaderCollection Headers { get; set; } = new WebHeaderCollection();

        /// <summary>
        /// Gets or sets the Keep-Alive value.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [keep alive]; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that 
        /// implements the IDisposable interface, and that object has been disposed
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot be changed after headers are sent.</exception>
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

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        /// <value>
        /// The output stream.
        /// </value>
        public ResponseStream OutputStream =>
            _outputStream ?? (_outputStream = _context.Connection.GetResponseStream());

        /// <summary>
        /// Gets or sets the protocol version.
        /// </summary>
        /// <value>
        /// The protocol version.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the 
        /// IDisposable interface, and that object has been disposed
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot be changed after headers are sent.</exception>
        /// <exception cref="System.ArgumentNullException">value</exception>
        /// <exception cref="System.ArgumentException">Must be 1.0 or 1.1 - value</exception>
        public Version ProtocolVersion
        {
            get => _version;

            set
            {
                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                if (HeadersSent)
                    throw new InvalidOperationException(CannotChangeHeaderWarning);

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value.Major != 1 || (value.Minor != 0 && value.Minor != 1))
                    throw new ArgumentException("Must be 1.0 or 1.1", nameof(value));

                if (_disposed)
                    throw new ObjectDisposedException(GetType().ToString());

                _version = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [send chunked].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [send chunked]; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the 
        /// IDisposable interface, and that object has been disposed
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

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        /// <exception cref="System.ObjectDisposedException">
        /// Is thrown when you try to access a member of an object that implements the 
        /// IDisposable interface, and that object has been disposed
        /// </exception>
        /// <exception cref="System.InvalidOperationException">Cannot be changed after headers are sent.</exception>
        /// <exception cref="System.Net.ProtocolViolationException">StatusCode must be between 100 and 999.</exception>
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
                    throw new System.Net.ProtocolViolationException("StatusCode must be between 100 and 999.");
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

        /// <summary>
        /// Adds the header.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Is thrown when a null reference is passed to a 
        /// method that does not accept it as a valid argument
        /// </exception>
        /// <exception cref="System.ArgumentException">'name' cannot be empty</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Is thrown when the value of an argument is outside the 
        /// allowable range of values as defined by the invoked method
        /// </exception>
        public void AddHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("'name' cannot be empty", nameof(name));

            // TODO: check for forbidden headers and invalid characters
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

        /// <summary>
        /// Sets the cookie.
        /// </summary>
        /// <param name="cookie">The cookie.</param>
        /// <exception cref="System.ArgumentNullException">
        ///  Is thrown when a null reference is passed to a method
        ///  that does not accept it as a valid argument
        /// </exception>
        /// <exception cref="System.ArgumentException">The cookie already exists.</exception>
        public void SetCookie(System.Net.Cookie cookie)
        {
            if (cookie == null)
                throw new ArgumentNullException(nameof(cookie));

            if (_cookies != null)
            {
                if (_cookies.Cast<Cookie>().Any(c =>
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
                    Headers.AddWithoutValidate("Content-Type", _contentType + "; charset=" + Encoding.UTF8.WebName);
                }
                else
                {
                    Headers.AddWithoutValidate("Content-Type", _contentType);
                }
            }

            if (Headers["Server"] == null)
                Headers.AddWithoutValidate("Server", "embedio/1.0");

            var inv = CultureInfo.InvariantCulture;
            if (Headers["Date"] == null)
                Headers.AddWithoutValidate("Date", DateTime.UtcNow.ToString("r", inv));

            if (!_chunked)
            {
                if (!_clSet && closing)
                {
                    _clSet = true;
                    _contentLength = 0;
                }

                if (_clSet)
                    Headers.AddWithoutValidate("Content-Length", _contentLength.ToString(inv));
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
                Headers.AddWithoutValidate("Connection", "close");
                connClose = true;
            }

            if (_chunked)
                Headers.AddWithoutValidate("Transfer-Encoding", "chunked");

            var reuses = _context.Connection.Reuses;
            if (reuses >= 100)
            {
                ForceCloseChunked = true;
                if (!connClose)
                {
                    Headers.AddWithoutValidate("Connection", "close");
                    connClose = true;
                }
            }

            if (!connClose)
            {
                Headers.AddWithoutValidate("Keep-Alive", $"timeout=15,max={100 - reuses}");
                if (_context.Request.ProtocolVersion <= HttpVersion.Version10)
                    Headers.AddWithoutValidate("Connection", "keep-alive");
            }

            if (_cookies != null)
            {
                foreach (System.Net.Cookie cookie in _cookies)
                    Headers.AddWithoutValidate("Set-Cookie", CookieToClientString(cookie));
            }

            var writer = new StreamWriter(ms, Encoding.UTF8, 256);
            writer.Write("HTTP/{0} {1} {2}\r\n", _version, _statusCode, StatusDescription);
            var headersStr = FormatHeaders(Headers);
            writer.Write(headersStr);
            writer.Flush();
            var preamble = Encoding.UTF8.GetPreamble().Length;
            if (_outputStream == null)
                _outputStream = _context.Connection.GetResponseStream();

            /* Assumes that the ms was at position 0 */
            ms.Position = preamble;
            HeadersSent = true;
        }

        private static string FormatHeaders(WebHeaderCollection headers)
        {
            var sb = new StringBuilder();

            foreach (var key in headers.AllKeys)
                sb.Append(key).Append(": ").Append(headers[key]).Append("\r\n");

            return sb.Append("\r\n").ToString();
        }

        private static string CookieToClientString(System.Net.Cookie cookie)
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

        private static string QuotedString(System.Net.Cookie cookie, string value)
            => cookie.Version == 0 || value.IsToken() ? value : "\"" + value.Replace("\"", "\\\"") + "\"";

        private void Close(bool force)
        {
            _disposed = true;

            _context.Connection.Close(force);
        }
    }
}
#endif
﻿namespace Unosquare.Net
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Labs.EmbedIO;
#if !NETSTANDARD1_3
    using System.Security.Cryptography.X509Certificates;
#endif

    /// <summary>
    /// Represents an HTTP Listener Request.
    /// </summary>
    internal sealed class HttpListenerRequest 
        : IHttpRequest
    {
        private static readonly byte[] HttpStatus100 = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");
        private static readonly char[] Separators = { ' ' };

        private readonly HttpListenerContext _context;
        private Encoding _contentEncoding;
        private bool _clSet;
        private CookieCollection _cookies;
        private Stream _inputStream;
        private Uri _url;
        private bool _kaSet;
        private bool _keepAlive;

#if !NETSTANDARD1_3
        delegate X509Certificate2 GccDelegate();
        GccDelegate _gccDelegate;
#endif

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            Headers = new NameValueCollection();
            ProtocolVersion = HttpVersion.Version10;
        }

        /// <summary>
        /// Gets the MIME accept types.
        /// </summary>
        /// <value>
        /// The accept types.
        /// </value>
        public string[] AcceptTypes { get; private set; }

        /// <inheritdoc />
        public Encoding ContentEncoding
        {
            get
            {
                if (_contentEncoding != null)
                    return _contentEncoding;

                var contentType = Headers["Content-Type"];

                if (!string.IsNullOrEmpty(contentType))
                {
                    _contentEncoding = HttpBase.GetEncoding(contentType);

                    if (_contentEncoding != null)
                        return _contentEncoding;
                }

                var defaultEncoding = Encoding.UTF8;
                var acceptCharset = Headers["Accept-Charset"]?.Split(Labs.EmbedIO.Constants.Strings.CommaSplitChar)
                    .Select(x => x.Trim().Split(';'))
                    .Select(x => new
                    {
                        Charset = x[0],
                        Q = x.Length == 1 ? 1m : decimal.Parse(x[1].Trim().Replace("q=", string.Empty)),
                    })
                    .OrderBy(x => x.Q)
                    .Select(x => x.Charset)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(acceptCharset) == false)
                {
                    defaultEncoding = Encoding.GetEncoding(acceptCharset);
                }

                return _contentEncoding = defaultEncoding;
            }
        }

        /// <inheritdoc />
        public long ContentLength64 { get; private set; }

        /// <inheritdoc />
        public string ContentType => Headers["content-type"];

        /// <inheritdoc />
        public ICookieCollection Cookies => _cookies ?? (_cookies = new CookieCollection());
        
        /// <inheritdoc />
        public bool HasEntityBody => ContentLength64 > 0;

        /// <inheritdoc />
        public NameValueCollection Headers { get; }

        /// <inheritdoc />
        public string HttpMethod { get; private set; }

        /// <inheritdoc />
        public Stream InputStream => _inputStream ??
                                     (_inputStream =
                                         ContentLength64 > 0 ? _context.Connection.GetRequestStream(ContentLength64) : Stream.Null);

        /// <inheritdoc />
        public bool IsAuthenticated => false;

        /// <inheritdoc />
        public bool IsLocal => LocalEndPoint?.Address?.Equals(RemoteEndPoint?.Address) ?? true;

#if !NETSTANDARD1_3
        /// <summary>
        /// Gets a value indicating whether this request is under a secure connection.
        /// </summary>
        public bool IsSecureConnection => _context.Connection.IsSecure;
#endif

        /// <inheritdoc />
        public bool KeepAlive
        {
            get
            {
                if (_kaSet)
                    return _keepAlive;

                _kaSet = true;

                // 1. Connection header
                // 2. Protocol (1.1 == keep-alive by default)
                // 3. Keep-Alive header
                var cnc = Headers["Connection"];
                if (!string.IsNullOrEmpty(cnc))
                {
                    _keepAlive = string.Compare(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase) == 0;
                }
                else if (ProtocolVersion == HttpVersion.Version11)
                {
                    _keepAlive = true;
                }
                else
                {
                    cnc = Headers["keep-alive"];

                    if (!string.IsNullOrEmpty(cnc))
                        _keepAlive = string.Compare(cnc, "closed", StringComparison.OrdinalIgnoreCase) != 0;
                }

                return _keepAlive;
            }
        }
        
        /// <inheritdoc />
        public IPEndPoint LocalEndPoint => _context.Connection.LocalEndPoint;

        /// <inheritdoc />
        public Version ProtocolVersion { get; private set; }

        /// <inheritdoc />
        public NameValueCollection QueryString { get; private set; }

        /// <inheritdoc />
        public string RawUrl { get; private set; }
        
        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint => _context.Connection.RemoteEndPoint;
        
        /// <inheritdoc />
        public Uri Url => _url;

        /// <inheritdoc />
        public Uri UrlReferrer { get; private set; }

        /// <inheritdoc />
        public string UserAgent => Headers["user-agent"];

        /// <inheritdoc />
        public Guid RequestTraceIdentifier => Guid.NewGuid();

        public string UserHostAddress => LocalEndPoint.ToString();

        public string UserHostName => Headers["host"];

        public string[] UserLanguages { get; private set; }
        
        /// <inheritdoc />
        public bool IsWebSocketRequest => HttpMethod == "GET" && ProtocolVersion > HttpVersion.Version10 && Headers.Contains("Upgrade", "websocket") && Headers.Contains("Connection", "Upgrade");

        internal void SetRequestLine(string req)
        {
            var parts = req.Split(Separators, 3);
            if (parts.Length != 3)
            {
                _context.ErrorMessage = "Invalid request line (parts).";
                return;
            }

            HttpMethod = parts[0];

            foreach (var c in HttpMethod)
            {
                var ic = (int)c;

                if ((ic >= 'A' && ic <= 'Z') ||
                    (ic > 32 && c < 127 && c != '(' && c != ')' && c != '<' &&
                     c != '<' && c != '>' && c != '@' && c != ',' && c != ';' &&
                     c != ':' && c != '\\' && c != '"' && c != '/' && c != '[' &&
                     c != ']' && c != '?' && c != '=' && c != '{' && c != '}'))
                    continue;

                _context.ErrorMessage = "(Invalid verb)";
                return;
            }

            RawUrl = parts[1];
            if (parts[2].Length != 8 || !parts[2].StartsWith("HTTP/"))
            {
                _context.ErrorMessage = "Invalid request line (version).";
                return;
            }

            try
            {
                ProtocolVersion = new Version(parts[2].Substring(5));

                if (ProtocolVersion.Major < 1)
                    throw new InvalidOperationException();
            }
            catch
            {
                _context.ErrorMessage = "Invalid request line (version).";
            }
        }

        internal void FinishInitialization()
        {
            var host = UserHostName;
            if (ProtocolVersion > HttpVersion.Version10 && string.IsNullOrEmpty(host))
            {
                _context.ErrorMessage = "Invalid host name";
                return;
            }

            Uri rawUri = null;
            var path = RawUrl.ToLowerInvariant().MaybeUri() && Uri.TryCreate(RawUrl, UriKind.Absolute, out rawUri)
                ? rawUri.PathAndQuery
                : RawUrl;

            if (string.IsNullOrEmpty(host))
                host = UserHostAddress;

            if (rawUri != null)
                host = rawUri.Host;

            var colon = host.LastIndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            // var baseUri = $"{(IsSecureConnection ? "https" : "http")}://{host}:{LocalEndPoint.Port}";
            var baseUri = $"http://{host}:{LocalEndPoint.Port}";

            if (!Uri.TryCreate(baseUri + path, UriKind.Absolute, out _url))
            {
                _context.ErrorMessage = WebUtility.HtmlEncode($"Invalid url: {baseUri}{path}");
                return;
            }

            CreateQueryString(_url.Query);

            if (!_clSet)
            {
                if (string.Compare(HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return;
                }
            }

            if (string.Compare(Headers["Expect"], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
            {
                _context.Connection.GetResponseStream().InternalWrite(HttpStatus100, 0, HttpStatus100.Length);
            }
        }

        internal void AddHeader(string header)
        {
            var colon = header.IndexOf(':');
            if (colon == -1 || colon == 0)
            {
                _context.ErrorMessage = "Bad Request";
                return;
            }

            var name = header.Substring(0, colon).Trim();
            var val = header.Substring(colon + 1).Trim();
            
            Headers.Set(name, val);

            switch (name.ToLowerInvariant())
            {
                case "accept-language":
                    UserLanguages = val.Split(Labs.EmbedIO.Constants.Strings.CommaSplitChar); // yes, only split with a ','
                    break;
                case "accept":
                    AcceptTypes = val.Split(Labs.EmbedIO.Constants.Strings.CommaSplitChar); // yes, only split with a ','
                    break;
                case "content-length":
                    try
                    {
                        // TODO: max. content_length?
                        ContentLength64 = long.Parse(val.Trim());
                        if (ContentLength64 < 0)
                            _context.ErrorMessage = "Invalid Content-Length.";
                        _clSet = true;
                    }
                    catch
                    {
                        _context.ErrorMessage = "Invalid Content-Length.";
                    }

                    break;
                case "referer":
                    try
                    {
                        UrlReferrer = new Uri(val);
                    }
                    catch
                    {
                        UrlReferrer = new Uri("http://someone.is.screwing.with.the.headers.com/");
                    }

                    break;
                case "cookie":
                    ParseCookies(val);

                    break;
            }
        }

        // returns true is the stream could be reused.
        internal async Task<bool> FlushInput()
        {
            if (!HasEntityBody)
                return true;

            var length = 2048;
            if (ContentLength64 > 0)
                length = (int)Math.Min(ContentLength64, length);

            var bytes = new byte[length];

            while (true)
            {
                try
                {
                    var data = await InputStream.ReadAsync(bytes, 0, length).ConfigureAwait(false);

                    if (data <= 0)
                        return true;
                }
                catch (ObjectDisposedException)
                {
                    _inputStream = null;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private void ParseCookies(string val)
        {
            if (_cookies == null)
                _cookies = new CookieCollection();

            var cookieStrings = val.Split(Labs.EmbedIO.Constants.Strings.CookieSplitChars)
                .Where(x => string.IsNullOrEmpty(x) == false);
            Cookie current = null;
            var version = 0;

            foreach (var str in cookieStrings)
            {
                if (str.StartsWith("$Version"))
                {
                    version = int.Parse(str.Substring(str.IndexOf('=') + 1).Unquote());
                }
                else if (str.StartsWith("$Path") && current != null)
                {
                    current.Path = str.Substring(str.IndexOf('=') + 1).Trim();
                }
                else if (str.StartsWith("$Domain") && current != null)
                {
                    current.Domain = str.Substring(str.IndexOf('=') + 1).Trim();
                }
                else if (str.StartsWith("$Port") && current != null)
                {
                    current.Port = str.Substring(str.IndexOf('=') + 1).Trim();
                }
                else
                {
                    if (current != null)
                    {
                        _cookies.Add(current);
                    }

                    current = new Cookie();
                    var idx = str.IndexOf('=');
                    if (idx > 0)
                    {
                        current.Name = str.Substring(0, idx).Trim();
                        current.Value = str.Substring(idx + 1).Trim();
                    }
                    else
                    {
                        current.Name = str.Trim();
                        current.Value = string.Empty;
                    }

                    current.Version = version;
                }
            }

            if (current != null)
            {
                _cookies.Add(current);
            }
        }
        
        private void CreateQueryString(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                QueryString = new NameValueCollection(1);
                return;
            }

            QueryString = new NameValueCollection();
            if (query[0] == '?')
                query = query.Substring(1);

            var components = query.Split('&');
            foreach (var kv in components)
            {
                var pos = kv.IndexOf('=');
                if (pos == -1)
                {
                    QueryString.Add(null, WebUtility.UrlDecode(kv));
                }
                else
                {
                    var key = WebUtility.UrlDecode(kv.Substring(0, pos));
                    var val = WebUtility.UrlDecode(kv.Substring(pos + 1));

                    QueryString.Add(key, val);
                }
            }
        }

#if !NETSTANDARD1_3
        /// <summary>
        /// Begins to the get client certificate asynchronously.
        /// </summary>
        /// <param name="requestCallback">The request callback.</param>
        /// <param name="state">The state.</param>
        /// <returns></returns>
        public IAsyncResult BeginGetClientCertificate(AsyncCallback requestCallback, object state)
        {
            if (_gccDelegate == null)
                _gccDelegate = GetClientCertificate;
            return _gccDelegate.BeginInvoke(requestCallback, state);
        }
        
        /// <summary>
        /// Finishes the get client certificate asynchronous operation.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">asyncResult</exception>
        /// <exception cref="System.InvalidOperationException"></exception>
        public X509Certificate2 EndGetClientCertificate(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (_gccDelegate == null)
                throw new InvalidOperationException();

            return _gccDelegate.EndInvoke(asyncResult);
        }
        
        /// <summary>
        /// Gets the client certificate.
        /// </summary>
        /// <returns></returns>
        public X509Certificate2 GetClientCertificate() => _context.Connection.ClientCertificate;
#endif
    }

    /// <summary>
    /// Define HTTP Versions.
    /// </summary>
    internal static class HttpVersion
    {
        /// <summary>
        /// The version 1.0.
        /// </summary>
        public static readonly Version Version10 = new Version(1, 0);

        /// <summary>
        /// The version 1.1.
        /// </summary>
        public static readonly Version Version11 = new Version(1, 1);
    }
}
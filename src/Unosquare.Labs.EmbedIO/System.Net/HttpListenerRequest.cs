#if !NET46
//
// System.Net.HttpListenerRequest
//
// Authors:
//	Gonzalo Paniagua Javier (gonzalo.mono@gmail.com)
//	Marek Safar (marek.safar@gmail.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2011-2012 Xamarin, Inc. (http://xamarin.com)
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

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
#if SSL
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
#endif

namespace Unosquare.Net
{
    /// <devdoc>
    ///    <para>
    ///       Defines the HTTP version number supported by the <see cref='System.Net.HttpWebRequest'/> and
    ///    <see cref='System.Net.HttpWebResponse'/> classes.
    ///    </para>
    /// </devdoc>
    public class HttpVersion
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly Version Version10 = new Version(1, 0);
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public static readonly Version Version11 = new Version(1, 1);

    }// class HttpVersion

    /// <summary>
    /// Represents an HTTP Listener's request
    /// </summary>
    public sealed class HttpListenerRequest
    {
        private Encoding _contentEncoding;
        private bool _clSet;
        private CookieCollection _cookies;
        private Stream _inputStream;
        private Uri _url;
        private readonly HttpListenerContext _context;
        private bool _isChunked = false;
        private bool _kaSet;
        private bool _keepAlive;

#if SSL
        delegate X509Certificate2 GccDelegate();
        GccDelegate _gccDelegate;
#endif

        private static readonly byte[] _100Continue = Encoding.UTF8.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");

        internal HttpListenerRequest(HttpListenerContext context)
        {
            _context = context;
            Headers = new NameValueCollection();
            ProtocolVersion = HttpVersion.Version10;
        }

        private static readonly char[] Separators = { ' ' };

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
                    throw new Exception();
            }
            catch
            {
                _context.ErrorMessage = "Invalid request line (version).";
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

        private static bool MaybeUri(string s)
        {
            var p = s.IndexOf(':');
            if (p == -1)
                return false;

            return p < 10 && IsPredefinedScheme(s.Substring(0, p));
        }

        //
        // Using a simple block of if's is twice as slow as the compiler generated
        // switch statement.   But using this tuned code is faster than the
        // compiler generated code, with a million loops on x86-64:
        //
        // With "http": .10 vs .51 (first check)
        // with "https": .16 vs .51 (second check)
        // with "foo": .22 vs .31 (never found)
        // with "mailto": .12 vs .51  (last check)
        private static bool IsPredefinedScheme(string scheme)
        {
            if (scheme == null || scheme.Length < 3)
                return false;

            var c = scheme[0];
            if (c == 'h')
                return (scheme == "http" || scheme == "https");
            if (c == 'f')
                return (scheme == "file" || scheme == "ftp");

            if (c == 'n')
            {
                c = scheme[1];
                if (c == 'e')
                    return (scheme == "news" || scheme == "net.pipe" || scheme == "net.tcp");

                return scheme == "nntp";
            }

            return (c == 'g' && scheme == "gopher") || (c == 'm' && scheme == "mailto");
        }

        internal void FinishInitialization()
        {
            var host = UserHostName;
            if (ProtocolVersion > HttpVersion.Version10 && string.IsNullOrEmpty(host))
            {
                _context.ErrorMessage = "Invalid host name";
                return;
            }

            string path;
            Uri rawUri = null;
            if (MaybeUri(RawUrl.ToLowerInvariant()) && Uri.TryCreate(RawUrl, UriKind.Absolute, out rawUri))
                path = rawUri.PathAndQuery;
            else
                path = RawUrl;

            if (string.IsNullOrEmpty(host))
                host = UserHostAddress;

            if (rawUri != null)
                host = rawUri.Host;

            var colon = host.IndexOf(':');
            if (colon >= 0)
                host = host.Substring(0, colon);

            var baseUri = $"{((IsSecureConnection) ? "https" : "http")}://{host}:{LocalEndPoint.Port}";

            if (!Uri.TryCreate(baseUri + path, UriKind.Absolute, out _url))
            {
                _context.ErrorMessage = WebUtility.HtmlEncode("Invalid url: " + baseUri + path);
                return;
            }

            CreateQueryString(_url.Query);

            // Use reference source HttpListenerRequestUriBuilder to process url.
            // Fixes #29927
            _url = HttpListenerRequestUriBuilder.GetRequestUri(RawUrl, _url.Scheme,
                                _url.Authority, _url.LocalPath, _url.Query);

#if CHUNKED
            if (ProtocolVersion >= HttpVersion.Version11)
            {
                var tEncoding = Headers["Transfer-Encoding"];
                _isChunked = (tEncoding != null && string.Compare(tEncoding, "chunked", StringComparison.OrdinalIgnoreCase) == 0);
                // 'identity' is not valid!
                if (tEncoding != null && !_isChunked)
                {
                    return;
                }
            }
#endif

            if (!_isChunked && !_clSet)
            {
                if (string.Compare(HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return;
                }
            }

            if (string.Compare(Headers["Expect"], "100-continue", StringComparison.OrdinalIgnoreCase) == 0)
            {
                var output = _context.Connection.GetResponseStream();
                output.InternalWrite(_100Continue, 0, _100Continue.Length);
            }
        }

        internal void AddHeader(string header)
        {
            var colon = header.IndexOf(':');
            if (colon == -1 || colon == 0)
            {
                _context.ErrorMessage = "Bad Request";
                _context.ErrorStatus = 400;
                return;
            }

            var name = header.Substring(0, colon).Trim();
            var val = header.Substring(colon + 1).Trim();
            var lower = name.ToLowerInvariant();
            Headers.Set(name, val);

            switch (lower)
            {
                case "accept-language":
                    UserLanguages = val.Split(','); // yes, only split with a ','
                    break;
                case "accept":
                    AcceptTypes = val.Split(','); // yes, only split with a ','
                    break;
                case "content-length":
                    try
                    {
                        //TODO: max. content_length?
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
                    if (_cookies == null)
                        _cookies = new CookieCollection();

                    var cookieStrings = val.Split(',', ';');
                    Cookie current = null;
                    var version = 0;
                    foreach (var cookieString in cookieStrings)
                    {
                        var str = cookieString.Trim();
                        if (str.Length == 0)
                            continue;
                        if (str.StartsWith("$Version"))
                        {
                            version = int.Parse(str.Substring(str.IndexOf('=') + 1).Unquote());
                        }
                        else if (str.StartsWith("$Path"))
                        {
                            if (current != null)
                                current.Path = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Domain"))
                        {
                            if (current != null)
                                current.Domain = str.Substring(str.IndexOf('=') + 1).Trim();
                        }
                        else if (str.StartsWith("$Port"))
                        {
                            if (current != null)
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
                    var data = await InputStream.ReadAsync(bytes, 0, length);

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

        /// <summary>
        /// Gets the MIME accept types.
        /// </summary>
        /// <value>
        /// The accept types.
        /// </value>
        public string[] AcceptTypes { get; private set; }

#if SSL
        /// <summary>
        /// Gets the client certificate error code.
        /// </summary>
        /// <value>
        /// The client certificate error.
        /// </value>
        /// <exception cref="System.InvalidOperationException">No client certificate</exception>
        public int ClientCertificateError
        {
            get
            {
                var cnc = _context.Connection;
                if (cnc.ClientCertificate == null)
                    throw new InvalidOperationException("No client certificate");
                var errors = cnc.ClientCertificateErrors;
                if (errors != null && errors.Length > 0)
                    return errors[0];
                return 0;
            }
        }
#endif

        /// <summary>
        /// Gets the content encoding.
        /// </summary>
        /// <value>
        /// The content encoding.
        /// </value>
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
                var acceptCharset = Headers["Accept-Charset"]?.Split(',')
                    .Select(x => x.Trim().Split(';'))
                    .Select(x => new
                    {
                        Charset = x[0],
                        Q = x.Length == 1 ? 1m : decimal.Parse(x[1].Trim().Replace("q=", string.Empty))
                    })
                    .OrderBy(x => x.Q)
                    .Select(x => x.Charset)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(acceptCharset) == false)
                {
                    defaultEncoding = Encoding.GetEncoding(acceptCharset);
                }

                return (_contentEncoding = defaultEncoding);
            }
        }

        /// <summary>
        /// Gets the content length in a 64-bit integer
        /// </summary>
        /// <value>
        /// The content length64.
        /// </value>
        public long ContentLength64 { get; private set; }

        /// <summary>
        /// Gets the MIME type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        public string ContentType => Headers["content-type"];

        /// <summary>
        /// Gets the cookies collection.
        /// </summary>
        /// <value>
        /// The cookies.
        /// </value>
        public CookieCollection Cookies => _cookies ?? (_cookies = new CookieCollection());

        /// <summary>
        /// Gets a value indicating whether this instance has entity body.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has entity body; otherwise, <c>false</c>.
        /// </value>
        public bool HasEntityBody => (ContentLength64 > 0 || _isChunked);

        /// <summary>
        /// Gets the request headers.
        /// </summary>
        public NameValueCollection Headers { get; }

        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        public string HttpMethod { get; private set; }

        /// <summary>
        /// Gets the input stream.
        /// </summary>
        public Stream InputStream
        {
            get
            {
                if (_inputStream == null)
                {
                    if (_isChunked || ContentLength64 > 0)
                        _inputStream = _context.Connection.GetRequestStream(_isChunked, ContentLength64);
                    else
                        _inputStream = Stream.Null;
                }

                return _inputStream;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this request is authenticated.
        /// </summary>
        public bool IsAuthenticated => false;

        /// <summary>
        /// Gets a value indicating whether this request is local.
        /// </summary>
        public bool IsLocal => LocalEndPoint.Address.Equals(RemoteEndPoint.Address);

        /// <summary>
        /// Gets a value indicating whether this request is under a secure connection.
        /// </summary>
        public bool IsSecureConnection => _context.Connection.IsSecure;

        /// <summary>
        /// Gets the Keep-Alive value for this request
        /// </summary>
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
                    _keepAlive = (0 == string.Compare(cnc, "keep-alive", StringComparison.OrdinalIgnoreCase));
                }
                else if (ProtocolVersion == HttpVersion.Version11)
                {
                    _keepAlive = true;
                }
                else
                {
                    cnc = Headers["keep-alive"];
                    if (!string.IsNullOrEmpty(cnc))
                        _keepAlive = (0 != string.Compare(cnc, "closed", StringComparison.OrdinalIgnoreCase));
                }
                return _keepAlive;
            }
        }

        /// <summary>
        /// Gets the local end point.
        /// </summary>
        public IPEndPoint LocalEndPoint => _context.Connection.LocalEndPoint;

        /// <summary>
        /// Gets the protocol version.
        /// </summary>
        public Version ProtocolVersion { get; private set; }

        /// <summary>
        /// Gets the query string.
        /// </summary>
        public NameValueCollection QueryString { get; private set; }

        /// <summary>
        /// Gets the raw URL.
        /// </summary>
        public string RawUrl { get; private set; }

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        public IPEndPoint RemoteEndPoint => _context.Connection.RemoteEndPoint;

        /// <summary>
        /// Gets the request trace identifier.
        /// </summary>
        public Guid RequestTraceIdentifier => Guid.Empty;

        /// <summary>
        /// Gets the URL.
        /// </summary>
        public Uri Url => _url;

        /// <summary>
        /// Gets the URL referrer.
        /// </summary>
        /// <value>
        /// The URL referrer.
        /// </value>
        public Uri UrlReferrer { get; private set; }

        /// <summary>
        /// Gets the user agent.
        /// </summary>
        public string UserAgent => Headers["user-agent"];

        /// <summary>
        /// Gets the user host address.
        /// </summary>
        public string UserHostAddress => LocalEndPoint.ToString();

        /// <summary>
        /// Gets the name of the user host.
        /// </summary>
        /// <value>
        /// The name of the user host.
        /// </value>
        public string UserHostName => Headers["host"];

        /// <summary>
        /// Gets the user languages.
        /// </summary>
        public string[] UserLanguages { get; private set; }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string ServiceName => null;
        
        /// <summary>
        /// Gets a value indicating whether this request is a web socket request.
        /// </summary>
        public bool IsWebSocketRequest => HttpMethod == "GET" && ProtocolVersion > HttpVersion.Version10 && Headers.Contains("Upgrade", "websocket") && Headers.Contains("Connection", "Upgrade");

#if SSL
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
        public X509Certificate2 GetClientCertificate()
        {
            return _context.Connection.ClientCertificate;
        }

        /// <summary>
        /// Gets the client certificate asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task<X509Certificate2> GetClientCertificateAsync()
        {
            return Task<X509Certificate2>.Factory.FromAsync(BeginGetClientCertificate, EndGetClientCertificate, null);
        }
#endif
    }
}
#endif
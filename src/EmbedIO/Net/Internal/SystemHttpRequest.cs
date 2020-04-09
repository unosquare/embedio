using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace EmbedIO.Net.Internal
{
    /// <summary>
    /// Represents a wrapper for HttpListenerContext.Request.
    /// </summary>
    /// <seealso cref="EmbedIO.IHttpRequest" />
    public class SystemHttpRequest : IHttpRequest
    {
        private readonly System.Net.HttpListenerRequest _request;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemHttpRequest"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public SystemHttpRequest(System.Net.HttpListenerContext context)
        {
            _request = context.Request;
            Enum.TryParse<HttpVerbs>(_request.HttpMethod.Trim(), true, out var verb);
            HttpVerb = verb;
            Cookies = new SystemCookieCollection(_request.Cookies);
            LocalEndPoint = _request.LocalEndPoint;
            RemoteEndPoint = _request.RemoteEndPoint;
        }

        /// <inheritdoc />
        public NameValueCollection Headers => _request.Headers;

        /// <inheritdoc />
        public Version ProtocolVersion => _request.ProtocolVersion;

        /// <inheritdoc />
        public bool KeepAlive => _request.KeepAlive;

        /// <inheritdoc />
        public ICookieCollection Cookies { get; }

        /// <inheritdoc />
        public string RawUrl => _request.RawUrl;

        /// <inheritdoc />
        public NameValueCollection QueryString => _request.QueryString;

        /// <inheritdoc />
        public string HttpMethod => _request.HttpMethod;

        /// <inheritdoc />
        public HttpVerbs HttpVerb { get; }

        /// <inheritdoc />
        public Uri Url => _request.Url;

        /// <inheritdoc />
        public bool HasEntityBody => _request.HasEntityBody;

        /// <inheritdoc />
        public Stream InputStream => _request.InputStream;

        /// <inheritdoc />
        public Encoding ContentEncoding
        {
            get
            {
                if (!_request.HasEntityBody || _request.ContentType == null)
                {
                    return WebServer.DefaultEncoding;
                }

                var charSet = HeaderUtility.GetCharset(ContentType);
                if (string.IsNullOrEmpty(charSet))
                {
                    return WebServer.DefaultEncoding;
                }

                try
                {
                    return Encoding.GetEncoding(charSet);
                }
                catch (ArgumentException)
                {
                    return WebServer.DefaultEncoding;
                }
            }
        }

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; }

        /// <inheritdoc />
        public bool IsSecureConnection => _request.IsSecureConnection;

        /// <inheritdoc />
        public bool IsLocal => _request.IsLocal;

        /// <inheritdoc />
        public string UserAgent => _request.UserAgent;

        /// <inheritdoc />
        public bool IsWebSocketRequest => _request.IsWebSocketRequest;

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public string ContentType => _request.ContentType;

        /// <inheritdoc />
        public long ContentLength64 => _request.ContentLength64;

        /// <inheritdoc />
        public bool IsAuthenticated => _request.IsAuthenticated;

        /// <inheritdoc />
        public Uri UrlReferrer => _request.UrlReferrer;
    }
}
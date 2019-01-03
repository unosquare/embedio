#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Text;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Represents a wrapper for HttpListenerContext.Request.
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.IHttpRequest" />
    public class HttpRequest : IHttpRequest
    {
        private readonly HttpListenerRequest _request;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequest"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public HttpRequest(HttpListenerContext context)
        {
            _request = context.Request;
            Cookies = new CookieCollection(_request.Cookies);
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
        public Uri Url => _request.Url;

        /// <inheritdoc />
        public bool HasEntityBody => _request.HasEntityBody;

        /// <inheritdoc />
        public Stream InputStream => _request.InputStream;

        /// <inheritdoc />
        public Encoding ContentEncoding => _request.ContentEncoding;

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint => _request.RemoteEndPoint;

        /// <inheritdoc />
        public bool IsLocal => _request.IsLocal;

        /// <inheritdoc />
        public string UserAgent => _request.UserAgent;

        /// <inheritdoc />
        public bool IsWebSocketRequest => _request.IsWebSocketRequest;

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint => _request.LocalEndPoint;

        /// <inheritdoc />
        public string ContentType => _request.ContentType;

        /// <inheritdoc />
        public long ContentLength64 => _request.ContentLength64;

        /// <inheritdoc />
        public bool IsAuthenticated => _request.IsAuthenticated;

        /// <inheritdoc />
        public Uri UrlReferrer => _request.UrlReferrer;

        /// <inheritdoc />
        public Guid RequestTraceIdentifier => _request.RequestTraceIdentifier;
    }
}
#endif
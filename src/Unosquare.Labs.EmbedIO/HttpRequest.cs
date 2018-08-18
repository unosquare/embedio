#if NET47
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Text;
    using System.Net;
    using System.Collections.Specialized;
    using System.IO;

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
        }

        /// <inheritdoc />
        public NameValueCollection Headers => _request.Headers;

        /// <inheritdoc />
        public Version ProtocolVersion => _request.ProtocolVersion;
        
        /// <inheritdoc />
        public bool KeepAlive => _request.KeepAlive;

        /// <inheritdoc />
        public CookieCollection Cookies => _request.Cookies;
        
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
        public string UserAgent { get; }
        
        /// <inheritdoc />
        public bool IsWebSocketRequest { get; set; }
        
        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }
        
        /// <inheritdoc />
        public string ContentType => _request.ContentType;
    }
}
#endif
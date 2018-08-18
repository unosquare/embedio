#if NET47
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Text;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Represents a wrapper for HttpListenerContext.Response.
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.IHttpResponse" />
    public class HttpResponse : IHttpResponse
    {
        private readonly HttpListenerResponse _response;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResponse"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public HttpResponse(HttpListenerContext context)
        {
            _response = context.Response;
        }

        /// <inheritdoc />
        public WebHeaderCollection Headers => _response.Headers;

        /// <inheritdoc />
        public int StatusCode
        {
            get => _response.StatusCode;
            set => _response.StatusCode = value;
        }

        /// <inheritdoc />
        public long ContentLength64
        {
            get => _response.ContentLength64;
            set => _response.ContentLength64 = value;
        }

        /// <inheritdoc />
        public string ContentType
        {
            get => _response.ContentType;
            set => _response.ContentType = value;
        }

        /// <inheritdoc />
        public Stream OutputStream => _response.OutputStream;

        /// <inheritdoc />
        public CookieCollection Cookies => _response.Cookies;

        /// <inheritdoc />
        public Encoding ContentEncoding
        {
            get => _response.ContentEncoding;
            set => _response.ContentEncoding = value;
        }
        
        /// <inheritdoc />
        public bool KeepAlive
        {
            get => _response.KeepAlive;
            set => _response.KeepAlive = value;
        }
        
        /// <inheritdoc />
        public Version ProtocolVersion
        {
            get => _response.ProtocolVersion;
            set => _response.ProtocolVersion = value;
        }

        /// <inheritdoc />
        public void AddHeader(string headerName, string value) => _response.AddHeader(headerName, value);

        /// <inheritdoc />
        public void SetCookie(Cookie sessionCookie) => _response.SetCookie(sessionCookie);
    }
}
#endif
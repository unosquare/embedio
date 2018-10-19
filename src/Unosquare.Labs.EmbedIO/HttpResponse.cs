#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Specialized;
    using System.Text;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Represents a wrapper for HttpListenerContext.Response.
    /// </summary>
    /// <seealso cref="IHttpResponse" />
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
            Cookies = new CookieCollection(_response.Cookies);
        }

        /// <inheritdoc />
        public NameValueCollection Headers => _response.Headers;

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
        public ICookieCollection Cookies { get; }

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

        /// <inheritdoc />
        public void Close()
        {
            _response.OutputStream?.Close();
            _response.OutputStream?.Dispose();
        }
    }
}
#endif
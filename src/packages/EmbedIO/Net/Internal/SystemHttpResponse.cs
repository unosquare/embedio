using System;
using System.IO;
using System.Net;
using System.Text;

namespace EmbedIO.Net.Internal
{
    /// <summary>
    /// Represents a wrapper for HttpListenerContext.Response.
    /// </summary>
    /// <seealso cref="IHttpResponse" />
    public class SystemHttpResponse : IHttpResponse
    {
        private readonly System.Net.HttpListenerResponse _response;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemHttpResponse"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public SystemHttpResponse(System.Net.HttpListenerContext context)
        {
            _response = context.Response;
            Cookies = new SystemCookieCollection(_response.Cookies);
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
        public ICookieCollection Cookies { get; }

        /// <inheritdoc />
        public Encoding? ContentEncoding
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
        public bool SendChunked
        {
            get => _response.SendChunked;
            set => _response.SendChunked = value;
        }

        /// <inheritdoc />
        public Version ProtocolVersion
        {
            get => _response.ProtocolVersion;
            set => _response.ProtocolVersion = value;
        }

        /// <inheritdoc />
        public string StatusDescription 
        {
            get => _response.StatusDescription;
            set => _response.StatusDescription = value;
        }

        /// <inheritdoc />
        public void SetCookie(Cookie cookie) => _response.SetCookie(cookie);

        /// <inheritdoc />
        public void Close() => _response.OutputStream?.Dispose();
    }
}
namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Represents an <c>IHttpResponse</c> implementation for unit testing.
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.IHttpResponse" />
    public class TestHttpResponse : IHttpResponse, IDisposable
    {
        /// <inheritdoc />
        public NameValueCollection Headers { get; }

        /// <inheritdoc />
        public int StatusCode { get; set; }

        /// <inheritdoc />
        public long ContentLength64 { get; set; }

        /// <inheritdoc />
        public string ContentType { get; set; }

        /// <inheritdoc />
        public Stream OutputStream { get; } = new MemoryStream();

        /// <inheritdoc />
        public ICookieCollection Cookies { get; }

        /// <inheritdoc />
        public Encoding ContentEncoding { get; set; }

        /// <inheritdoc />
        public bool KeepAlive { get; set; }

        /// <inheritdoc />
        public Version ProtocolVersion { get; set; }

        /// <inheritdoc />
        public void AddHeader(string headerName, string value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SetCookie(Cookie sessionCookie)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            OutputStream?.Dispose();
        }
    }
}

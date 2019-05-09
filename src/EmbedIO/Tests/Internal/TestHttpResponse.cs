﻿namespace EmbedIO.Tests.Internal
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Represents an <c>IHttpResponse</c> implementation for unit testing.
    /// </summary>
    /// <seealso cref="IHttpResponse" />
    internal class TestHttpResponse : IHttpResponse, IDisposable
    {
        /// <inheritdoc />
        /// <remarks>
        /// <para>The <see cref="WebHeaderCollection"/> used by this class is not exactly
        /// the same as the one used by <see cref="HttpResponse"/></para>
        /// </remarks>
        public WebHeaderCollection Headers { get; } = new WebHeaderCollection();

        /// <inheritdoc />
        public int StatusCode { get; set; } = (int)HttpStatusCode.OK;

        /// <inheritdoc />
        public long ContentLength64 { get; set; }

        /// <inheritdoc />
        public string ContentType { get; set; }

        /// <inheritdoc />
        public Stream OutputStream { get; } = new MemoryStream();

        /// <inheritdoc />
        public ICookieCollection Cookies { get; } = new Net.CookieCollection();

        /// <inheritdoc />
        public Encoding ContentEncoding { get; } = Encoding.UTF8;

        /// <inheritdoc />
        public bool KeepAlive { get; set; }

        /// <inheritdoc />
        public Version ProtocolVersion { get; } = Net.HttpVersion.Version11;

        /// <summary>
        /// Gets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public byte[] Body { get; private set; }

        /// <inheritdoc />
        public string StatusDescription { get; set; }
        
        internal bool IsClosed { get; private set; }

        /// <inheritdoc />
        public void AddHeader(string headerName, string value) => Headers.Add(headerName, value);

        /// <inheritdoc />
        public void SetCookie(Cookie sessionCookie) => Cookies.Add(sessionCookie);

        /// <inheritdoc />
        public void Close()
        {
            IsClosed = true;
            Body = (OutputStream as MemoryStream)?.ToArray();

            Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            OutputStream?.Dispose();
        }

        /// <summary>
        /// Gets the body as string.
        /// </summary>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A string from the body.</returns>
        public string GetBodyAsString(Encoding encoding = null)
        {
            if (!(OutputStream is MemoryStream ms)) return null;

            var result = (encoding ?? Encoding.UTF8).GetString(ms.ToArray());

            // Remove BOM
            return result.Length > 0 && result[0] == 65279 ? result.Remove(0, 1) : result;
        }
    }
}

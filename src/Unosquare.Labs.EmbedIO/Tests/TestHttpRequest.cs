﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Text;
    using Constants;

    /// <summary>
    /// Represents an <c>IHttpRequest</c> implementation for unit testing.
    /// </summary>
    /// <seealso cref="IHttpRequest" />
    public class TestHttpRequest : IHttpRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpRequest" /> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="httpMethod">The HTTP method.</param>
        public TestHttpRequest(string url, HttpVerbs httpMethod = HttpVerbs.Get)
        {
            HttpMethod = httpMethod.ToString();
            Url = new Uri(url);
            RawUrl = url;
        }

        /// <inheritdoc />
        public NameValueCollection Headers { get; } = new NameValueCollection();

        /// <inheritdoc />
        public Version ProtocolVersion { get; } = Net.HttpVersion.Version11;

        /// <inheritdoc />
        public bool KeepAlive { get; }

        /// <inheritdoc />
        public ICookieCollection Cookies { get; }

        /// <inheritdoc />
        public string RawUrl { get; }

        /// <inheritdoc />
        public NameValueCollection QueryString { get; } = new NameValueCollection();

        /// <inheritdoc />
        public string HttpMethod { get; }

        /// <inheritdoc />
        public Uri Url { get; }

        /// <inheritdoc />
        public bool HasEntityBody { get; }

        /// <inheritdoc />
        public Stream InputStream { get; }

        /// <inheritdoc />
        public Encoding ContentEncoding { get; }

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; }

        /// <inheritdoc />
        public bool IsLocal { get; } = true;

        /// <inheritdoc />
        public string UserAgent { get; }

        /// <inheritdoc />
        public bool IsWebSocketRequest { get; }

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public string ContentType { get; }

        /// <inheritdoc />
        public long ContentLength64 { get; }

        /// <inheritdoc />
        public bool IsAuthenticated { get; }

        /// <inheritdoc />
        public Uri UrlReferrer { get; }
    }

}

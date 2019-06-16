using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace EmbedIO.Tests
{
    /// <summary>
    /// Represents an <c>IHttpRequest</c> implementation for unit testing.
    /// </summary>
    /// <seealso cref="IHttpRequest" />
    public class TestHttpRequest : IHttpRequest
    {
        private const string DefaultTestUrl = "http://test/";

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpRequest"/> class.
        /// </summary>
        /// <param name="httpVerb">The HTTP method.</param>
        public TestHttpRequest(HttpVerbs httpVerb = HttpVerbs.Get)
            : this(DefaultTestUrl, httpVerb)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpRequest" /> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="httpVerb">The HTTP method.</param>
        public TestHttpRequest(string url, HttpVerbs httpVerb = HttpVerbs.Get)
        {
            RawUrl = url ?? throw new ArgumentNullException(nameof(url));

            HttpVerb = httpVerb;
            HttpMethod = httpVerb.ToString();
            Url = new Uri(url);
        }

        /// <inheritdoc />
        public NameValueCollection Headers { get; } = new NameValueCollection();

        /// <inheritdoc />
        public Version ProtocolVersion { get; } = HttpVersion.Version11;

        /// <inheritdoc />
        public bool KeepAlive { get; } = false;

        /// <inheritdoc />
        public ICookieCollection Cookies { get; }

        /// <inheritdoc />
        public string RawUrl { get; }

        /// <inheritdoc />
        public NameValueCollection QueryString { get; } = new NameValueCollection();

        /// <inheritdoc />
        public string HttpMethod { get; }

        /// <inheritdoc />
        public HttpVerbs HttpVerb { get; }

        /// <inheritdoc />
        public Uri Url { get; }

        /// <inheritdoc />
        public bool HasEntityBody { get; }

        /// <inheritdoc />
        public Stream InputStream { get; }

        /// <inheritdoc />
        public Encoding ContentEncoding { get; }

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; } = new IPEndPoint(IPAddress.Loopback, 12345);

        /// <inheritdoc />
        public bool IsLocal => true;

        /// <inheritdoc />
        public bool IsSecureConnection => false;

        /// <inheritdoc />
        public string UserAgent { get; } = "EmbedIOTest/1.0";

        /// <inheritdoc />
        public bool IsWebSocketRequest { get; }

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public string ContentType { get; }

        /// <inheritdoc />
        public long ContentLength64 { get; } = 0;

        /// <inheritdoc />
        public bool IsAuthenticated { get; } = false;

        /// <inheritdoc />
        public Uri UrlReferrer { get; }
    }
}
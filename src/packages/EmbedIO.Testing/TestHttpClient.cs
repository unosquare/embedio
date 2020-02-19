using System;
using System.Net;
using System.Net.Http;
using EmbedIO.Testing.Internal;
using EmbedIO.Utilities;

namespace EmbedIO.Testing
{
    /// <summary>
    /// A <see cref="HttpClient"/> that can send requests
    /// either to an <see cref="ITestWebServer"/> interface,
    /// or to a web server on the network.
    /// </summary>
    public sealed class TestHttpClient : HttpClient
    {
        private TestHttpClient(TestMessageHandler handler, Uri baseUrl)
            : base(handler, true)
        {
            BaseAddress = baseUrl;
            CookieContainer = handler.CookieContainer;
        }

        private TestHttpClient(HttpClientHandler handler, string baseUrl)
            : this(handler, new Uri(baseUrl))
        {
        }

        private TestHttpClient(HttpClientHandler handler, Uri baseUrl)
            : base(handler, true)
        {
            BaseAddress = baseUrl;
            CookieContainer = handler.CookieContainer;
        }

        /// <summary>
        /// Creates a test client that communicates with the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <seealso cref="Create(string)"/>
        /// <seealso cref="Create(Uri)"/>
        public static TestHttpClient Create(ITestWebServer server)
        {
            Validate.NotNull(nameof(server), server);

#pragma warning disable CA2000 // Dispose created handler before it goes out of scope - HttpClient.Dispose will do it at due time.
            return new TestHttpClient(new TestMessageHandler(server), server.BaseUrl);
#pragma warning restore CA2000
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <seealso cref="Create(ITestWebServer)"/>
        /// <seealso cref="Create(Uri)"/>
        public static TestHttpClient Create(string baseUrl)
        {
            Validate.Url(nameof(baseUrl), baseUrl, UriKind.Absolute, true);

#pragma warning disable CA2000 // Dispose created handler before it goes out of scope - HttpClient.Dispose will do it at due time.
            return new TestHttpClient(new HttpClientHandler(), baseUrl);
#pragma warning restore CA2000
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <seealso cref="Create(ITestWebServer)"/>
        /// <seealso cref="Create(string)"/>
        public static TestHttpClient Create(Uri baseUrl)
        {
#pragma warning disable CA2000 // Dispose created handler before it goes out of scope - HttpClient.Dispose will do it at due time.
            return new TestHttpClient(new HttpClientHandler(), baseUrl);
#pragma warning restore CA2000
        }

        /// <summary>
        /// Gets the cookie container used to store server cookies.
        /// </summary>
        public CookieContainer CookieContainer { get; }
    }
}
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
        private TestHttpClient(TestMessageHandler handler, string baseUrl)
            : base(handler, true)
        {
            BaseAddress = new Uri(baseUrl);
            CookieContainer = handler.CookieContainer;
        }

        private TestHttpClient(HttpClientHandler handler, string baseUrl)
            : base(handler, true)
        {
            BaseAddress = new Uri(baseUrl);
            CookieContainer = handler.CookieContainer;
        }

        /// <summary>
        /// Gets the cookie container used to store server cookies.
        /// </summary>
        public CookieContainer CookieContainer { get; }

        /// <summary>
        /// Creates a test client that communicates with the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <seealso cref="Create(string)"/>
        public static TestHttpClient Create(ITestWebServer server)
        {
            var handler = new TestMessageHandler(Validate.NotNull(nameof(server), server));
            return new TestHttpClient(handler, server.BaseUrl);
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <seealso cref="Create(ITestWebServer)"/>
        public static TestHttpClient Create(string baseUrl)
        {
            var handler = new HttpClientHandler();
            return new TestHttpClient(handler, baseUrl);
        }
    }
}
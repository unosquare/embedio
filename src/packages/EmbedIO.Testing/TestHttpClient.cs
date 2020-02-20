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
        /// <exception cref="ArgumentNullException"><paramref name="server"/> is <see langword="null"/>.</exception>
        /// <seealso cref="Create(string)"/>
        /// <seealso cref="Create(Uri)"/>
        public static TestHttpClient Create(ITestWebServer server)
        {
            Validate.NotNull(nameof(server), server);

#pragma warning disable CA2000 // Dispose handler before it goes out of scope - Ownership of handler is transferred to the TestHttpClient instance.
            var handler = new TestMessageHandler(server);
#pragma warning restore CA2000
            try
            {
                return new TestHttpClient(handler, server.BaseUrl);
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="baseUrl"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="baseUrl"/> is not a valid absolute URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="baseUrl"/>'s scheme is neither <c>http</c> nor <c>https</c>.</para>
        /// </exception>
        /// <seealso cref="Create(ITestWebServer)"/>
        /// <seealso cref="Create(string, Action{HttpClientHandler})"/>
        /// <seealso cref="Create(Uri)"/>
        /// <seealso cref="Create(Uri, Action{HttpClientHandler})"/>
        public static TestHttpClient Create(string baseUrl)
        {
            Validate.Url(nameof(baseUrl), baseUrl, UriKind.Absolute, true);

#pragma warning disable CA2000 // Dispose handler before it goes out of scope - Ownership of handler is transferred to the TestHttpClient instance.
            var handler = new HttpClientHandler();
#pragma warning restore CA2000
            try
            {
#pragma warning disable CA5399 // CheckCertificateRevocationList not enabled on handler - Mostly fine for testing purposes.
                return new TestHttpClient(handler, baseUrl);
#pragma warning restore CA5399
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <param name="configureHandler">A callback used to configure the client's
        /// handler before creating the client.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="baseUrl"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="configureHandler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="baseUrl"/> is not a valid absolute URL.</para>
        /// <para>- or -</para>
        /// <para><paramref name="baseUrl"/>'s scheme is neither <c>http</c> nor <c>https</c>.</para>
        /// </exception>
        /// <seealso cref="Create(ITestWebServer)"/>
        /// <seealso cref="Create(string)"/>
        /// <seealso cref="Create(Uri)"/>
        /// <seealso cref="Create(Uri, Action{HttpClientHandler})"/>
        public static TestHttpClient Create(string baseUrl, Action<HttpClientHandler> configureHandler)
        {
            Validate.Url(nameof(baseUrl), baseUrl, UriKind.Absolute, true);
            Validate.NotNull(nameof(configureHandler), configureHandler);

#pragma warning disable CA2000 // Dispose handler before it goes out of scope - Ownership of handler is transferred to the TestHttpClient instance.
            var handler = new HttpClientHandler();
#pragma warning restore CA2000
            try
            {
                configureHandler(handler);
#pragma warning disable CA5399 // CheckCertificateRevocationList not enabled on handler - configureHandler can do that, if needed.
                return new TestHttpClient(handler, baseUrl);
#pragma warning restore CA5399
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="baseUrl"/> is <see langword="null"/>.</exception>
        /// <seealso cref="Create(ITestWebServer)"/>
        /// <seealso cref="Create(string)"/>
        /// <seealso cref="Create(string, Action{HttpClientHandler})"/>
        /// <seealso cref="Create(Uri, Action{HttpClientHandler})"/>
        public static TestHttpClient Create(Uri baseUrl)
        {
            Validate.NotNull(nameof(baseUrl), baseUrl);

#pragma warning disable CA2000 // Dispose handler before it goes out of scope - Ownership of handler is transferred to the TestHttpClient instance.
            var handler = new HttpClientHandler();
#pragma warning restore CA2000
            try
            {
#pragma warning disable CA5399 // CheckCertificateRevocationList not enabled on handler - Mostly fine for testing purposes.
                return new TestHttpClient(handler, baseUrl);
#pragma warning restore CA5399
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates a test client that communicates over the network
        /// (typically with a <see cref="WebServer"/>).
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <param name="configureHandler">A callback used to configure the client's
        /// handler before creating the client.</param>
        /// <returns>A newly-created <see cref="TestHttpClient"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="baseUrl"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="configureHandler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <seealso cref="Create(ITestWebServer)"/>
        /// <seealso cref="Create(string)"/>
        /// <seealso cref="Create(string, Action{HttpClientHandler})"/>
        /// <seealso cref="Create(Uri)"/>
        public static TestHttpClient Create(Uri baseUrl, Action<HttpClientHandler> configureHandler)
        {
            Validate.NotNull(nameof(baseUrl), baseUrl);
            Validate.NotNull(nameof(configureHandler), configureHandler);

#pragma warning disable CA2000 // Dispose handler before it goes out of scope - Ownership of handler is transferred to the TestHttpClient instance.
            var handler = new HttpClientHandler();
#pragma warning restore CA2000
            try
            {
                configureHandler(handler);
#pragma warning disable CA5399 // CheckCertificateRevocationList not enabled on handler - configureHandler can do that, if needed.
                return new TestHttpClient(handler, baseUrl);
#pragma warning restore CA5399
            }
            catch
            {
                handler.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Gets the cookie container used to store server cookies.
        /// </summary>
        public CookieContainer CookieContainer { get; }
    }
}
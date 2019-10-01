using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Testing
{
    /// <summary>
    /// <para>A Web server that does not actually communicate over the network;
    /// instead, it manages an internal queue of requests that simulate
    /// incoming connections.</para>
    /// <para>Requests can be forwarded to the server using the <see cref="HttpClient"/> instance
    /// returned by the <see cref="Client"/> property.</para>
    /// </summary>
    public class TestWebServer : WebServerBase<TestWebServerOptions>, ITestWebServer
    {
        /// <summary>
        /// The base URL that a <see cref="TestWebServer"/>, by default, simulates being bound to.
        /// </summary>
        public const string DefaultBaseUrl = "http://test.example.com:8080/";

        private CancellationTokenSource _internalCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestWebServer"/> class.
        /// </summary>
        /// <param name="baseUrl"></param>
        public TestWebServer(string baseUrl = DefaultBaseUrl)
        {
            BaseUrl = Validate.NotNullOrEmpty(nameof(baseUrl), baseUrl);
            Client = TestHttpClient.Create(this);
        }

        /// <summary>
        /// <para>Gets a <see cref="HttpClient"/> that communicates with this server.</para>
        /// <para>The returned client is already initialized with a base address,
        /// so requests URLs may omit the scheme and host parts.</para>
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>
        /// <para>Gets a <see cref="TestHttpClient"/> that communicates with this server.</para>
        /// <para>The returned client is already initialized with a base address,
        /// so requests URLs may omit the scheme and host parts.</para>
        /// </summary>
        public TestHttpClient Client { get; }

        /// <summary>
        /// Encapsulates the creation and use of a <see cref="TestWebServer"/>.
        /// </summary>
        /// <param name="configure">A callback used to configure the server.</param>
        /// <param name="use">A callback used to pass requests to the server.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="configure"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="use"/> is <see langword="null"/>.</para>
        /// </exception>
        public static async Task UseAsync(Action<IWebServer> configure, Func<HttpClient, Task> use)
        {
            Validate.NotNull(nameof(configure), configure);
            Validate.NotNull(nameof(use), use);

            using var server = new TestWebServer();
            configure(server);
            using var cancellationTokenSource = new CancellationTokenSource();
            server.Start(cancellationTokenSource.Token);
            await use(server.Client).ConfigureAwait(false);
            cancellationTokenSource.Cancel();
        }

        /// <inheritdoc />
        protected override void Prepare(CancellationToken cancellationToken)
        {
            base.Prepare(cancellationToken);

            _internalCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        /// <inheritdoc />
        protected override Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            // Since there's nothing to listen to, just wait for the server to be stopped.
            _internalCancellationTokenSource.Token.WaitHandle.WaitOne();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_internalCancellationTokenSource != null)
                {
                    if (!_internalCancellationTokenSource.IsCancellationRequested)
                        _internalCancellationTokenSource.Cancel();

                    _internalCancellationTokenSource.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void OnFatalException()
        {
            if (!(_internalCancellationTokenSource?.IsCancellationRequested ?? true))
                _internalCancellationTokenSource.Cancel();
        }
    }
}
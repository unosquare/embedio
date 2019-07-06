using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Testing.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Testing
{
    /// <summary>
    /// <para>A Web server that does not actually communicate over the network;
    /// instead, it manages an internal queue of requests that simulate
    /// incoming connections.</para>
    /// <para>Requests can be forwarded to the server using the <see cref="HttpClient"/> instance
    /// returned by the <see cref="Client"/> property.</para>
    /// </summary>
    public class TestWebServer : WebServerBase<TestWebServerOptions>
    {
        /// <summary>
        /// The base URL that a <see cref="TestWebServer"/>, by default, simulates being bound to.
        /// </summary>
        public const string DefaultBaseUrl = "http://test.example.com:8080/";

        private readonly Queue<IHttpContextImpl> _contexts = new Queue<IHttpContextImpl>();

        private bool _listening;

        private TaskCompletionSource<IHttpContextImpl> _pendingDequeue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestWebServer"/> class.
        /// </summary>
        /// <param name="baseUrl"></param>
        public TestWebServer(string baseUrl = DefaultBaseUrl)
        {
            Validate.NotNullOrEmpty(nameof(baseUrl), baseUrl);

            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;
            Client = new HttpClient(new TestMessageHandler(this), true) {
                BaseAddress = new Uri(baseUrl)
            };

            _listening = true;
        }

        /// <summary>
        /// <para>Gets a <see cref="HttpClient"/> that communicates with this server.</para>
        /// <para>The returned client is already initialized with a base address,
        /// so requests URLs may omit the scheme and host parts.</para>
        /// </summary>
        public HttpClient Client { get; }

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

            using (var server = new TestWebServer())
            {
                configure(server);
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
#pragma warning disable CS4014 // Call is not awaited - it is expected to run ion parallel.
                    Task.Run(() => server.RunAsync(cancellationTokenSource.Token));
#pragma warning restore CS4014
                    await use(server.Client).ConfigureAwait(false);
                    cancellationTokenSource.Cancel();
                }
            }
        }

        internal void EnqueueContext(IHttpContextImpl context)
        {
            if (!_listening)
                throw new InvalidOperationException("Web server is not listening any longer.");

            TaskCompletionSource<IHttpContextImpl> currentDequeue = null;
            lock (_contexts)
            {
                if (_pendingDequeue != null)
                {
                    currentDequeue = _pendingDequeue;
                    _pendingDequeue = null;
                }
                else
                {
                    _contexts.Enqueue(context);
                }
            }

            currentDequeue?.SetResult(context);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                TaskCompletionSource<IHttpContextImpl> currentDequeue = null;
                lock (_contexts)
                {
                    if (_pendingDequeue != null)
                    {
                        currentDequeue = _pendingDequeue;
                        _pendingDequeue = null;
                    }
                }

                currentDequeue?.SetException(new ObjectDisposedException(nameof(TestWebServer)));
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override bool ShouldProcessMoreRequests()
        {
            lock (_contexts)
            {
                return _listening;
            }
        }

        /// <inheritdoc />
        protected override Task<IHttpContextImpl> GetContextAsync(CancellationToken cancellationToken)
        {
            lock (_contexts)
            {
                if (_contexts.Count > 0)
                {
                    return Task.FromResult(_contexts.Dequeue());
                }

                if (_pendingDequeue != null)
                    throw new InvalidOperationException("Trying to dequeue two contexts at the same time.");

                _pendingDequeue = new TaskCompletionSource<IHttpContextImpl>();
                return _pendingDequeue.Task;
            }
        }

        /// <inheritdoc />
        protected override void OnFatalException()
        {
            lock (_contexts)
            {
                _listening = false;
            }
        } 
    }
}
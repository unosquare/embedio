namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;
    using Swan;
    using Core;

    /// <summary>
    /// Represents our tiny web server used to handle requests for testing.
    ///
    /// Use this <c>IWebServer</c> implementation to run your unit tests.
    /// </summary>
    public class TestWebServer : IWebServer, IDisposable
    {
        private readonly WebModules _modules = new WebModules();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestWebServer"/> class.
        /// </summary>
        /// <param name="routingStrategy">The routing strategy.</param>
        public TestWebServer(RoutingStrategy routingStrategy = RoutingStrategy.Wildcard)
        {
            Terminal.Settings.DisplayLoggingMessageType = LogMessageType.None;

            RoutingStrategy = routingStrategy;
            State = WebServerState.Listening;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="TestWebServer"/> class.
        /// </summary>
        ~TestWebServer()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public event WebServerStateChangedEventHandler StateChanged;

        /// <inheritdoc />
        public ISessionWebModule SessionModule => _modules.SessionModule;

        /// <inheritdoc />
        public RoutingStrategy RoutingStrategy { get; }

        /// <inheritdoc />
        public ReadOnlyCollection<IWebModule> Modules => _modules.Modules;
        
        /// <inheritdoc />
        public Func<IHttpContext, Task<bool>> OnMethodNotAllowed { get; set; } = ctx =>
            ctx.HtmlResponseAsync(Responses.Response405Html, System.Net.HttpStatusCode.MethodNotAllowed);

        /// <inheritdoc />
        public Func<IHttpContext, Task<bool>> OnNotFound { get; set; } = ctx =>
            ctx.HtmlResponseAsync(Responses.Response404Html, System.Net.HttpStatusCode.NotFound);

        /// <inheritdoc />
        public Func<IHttpContext, Exception, CancellationToken, Task<bool>> UnhandledException { get; set; }

        /// <summary>
        /// Gets the HTTP contexts.
        /// </summary>
        /// <value>
        /// The HTTP contexts.
        /// </value>
        public ConcurrentQueue<IHttpContext> HttpContexts { get; } = new ConcurrentQueue<IHttpContext>();

        /// <inheritdoc />
        public WebServerState State { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TestWebServer"/> has been disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if disposed; otherwise, <c>false</c>.
        /// </value>
        protected bool Disposed { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public T Module<T>()
            where T : class, IWebModule
        {
            return _modules.Module<T>();
        }

        /// <inheritdoc />
        public void RegisterModule(IWebModule webModule) => _modules.RegisterModule(webModule, this);

        /// <inheritdoc />
        public void UnregisterModule(Type moduleType) => _modules.UnregisterModule(moduleType);

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                var clientSocket = await GetContextAsync(ct).ConfigureAwait(false);

                if (ct.IsCancellationRequested || clientSocket == null)
                    return;

                // Usually we don't wait, but for testing let's do it.
                var handler = new HttpHandler(clientSocket);
                await handler.HandleClientRequest(ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the test HTTP Client.
        /// </summary>
        /// <returns>A new instance of the TestHttpClient.</returns>
        public TestHttpClient GetClient() => new TestHttpClient(this);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || Disposed) return;

            try
            {
                _modules.Dispose();
            }
            finally
            {
                Disposed = true;
            }
        }

        private async Task<IHttpContext> GetContextAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (HttpContexts.TryDequeue(out var entry)) return entry;

                await Task.Delay(100, ct).ConfigureAwait(false);
            }

            return null;
        }
    }
}

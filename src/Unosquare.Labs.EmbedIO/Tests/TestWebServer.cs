﻿namespace Unosquare.Labs.EmbedIO.Tests
{
    using Constants;
    using Swan;
    using System;
    using System.Collections.Concurrent;
    using Modules;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents our tiny web server used to handle requests for testing.
    ///
    /// Use this <c>IWebServer</c> implementation to run your unit tests.
    /// </summary>
    public class TestWebServer : IWebServer
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
        }

        /// <inheritdoc />
        public ISessionWebModule SessionModule => _modules.SessionModule;

        /// <inheritdoc />
        public RoutingStrategy RoutingStrategy { get; }

        /// <inheritdoc />
        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();
        
        /// <inheritdoc />
        public Func<IHttpContext, Task<bool>> OnMethodNotAllowed { get; set; }
        
        /// <inheritdoc />
        public Func<IHttpContext, Task<bool>> OnNotFound { get; set; }

        /// <summary>
        /// Gets the HTTP contexts.
        /// </summary>
        /// <value>
        /// The HTTP contexts.
        /// </value>
        public ConcurrentQueue<IHttpContext> HttpContexts { get; } = new ConcurrentQueue<IHttpContext>();
        
        /// <inheritdoc />
        public T Module<T>()
            where T : class, IWebModule
        {
            return _modules.Module<T>();
        }

        /// <inheritdoc />
        public void RegisterModule(IWebModule module) => _modules.RegisterModule(module, this);

        /// <inheritdoc />
        public void UnregisterModule(Type moduleType) => _modules.UnregisterModule(moduleType);

        /// <inheritdoc />
        public async Task RunAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                var clientSocket = await GetContextAsync(ct);

                if (ct.IsCancellationRequested || clientSocket == null)
                    return;

                // Usually we don't wait, but for testing let's do it.
                var handler = new HttpHandler(clientSocket);
                await handler.HandleClientRequest(ct);
            }
        }

        /// <summary>
        /// Called when any unhandled request.
        ///
        /// Any verb and any path.
        /// </summary>
        /// <param name="action">The action.</param>
        public void OnAny(Func<IHttpContext, CancellationToken, bool> action)
        {
            RegisterModule(new FallbackModule(action));
        }

        /// <summary>
        /// Gets the test HTTP Client.
        /// </summary>
        /// <returns>A new instance of the TestHttpClient.</returns>
        public TestHttpClient GetClient() => new TestHttpClient(this);

        private async Task<IHttpContext> GetContextAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (HttpContexts.TryDequeue(out var entry)) return entry;

                await Task.Delay(100, ct);
            }

            return null;
        }
    }
}

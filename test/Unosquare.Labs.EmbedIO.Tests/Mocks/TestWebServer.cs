namespace Unosquare.Labs.EmbedIO.Tests.Mocks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    internal class TestWebServer : IWebServer
    {
        private readonly ConcurrentQueue<HttpListenerContext> _entryQueue = new ConcurrentQueue<HttpListenerContext>();

        private readonly WebModules _modules = new WebModules();

        public TestWebServer(RoutingStrategy routingStrategy = RoutingStrategy.Wildcard)
        {
            RoutingStrategy = routingStrategy;
        }

        public ISessionWebModule SessionModule => _modules.SessionModule;
        public RoutingStrategy RoutingStrategy { get; }

        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();
        public Func<HttpListenerContext, Task<bool>> OnMethodNotAllowed { get; set; }
        public Func<HttpListenerContext, Task<bool>> OnNotFound { get; set; }

        public T Module<T>()
            where T : class, IWebModule
        {
            return _modules.Module<T>();
        }

        public void RegisterModule(IWebModule module) => _modules.RegisterModule(module, this);

        public void UnregisterModule(Type moduleType) => _modules.UnregisterModule(moduleType);

        public async Task RunAsync(CancellationToken ct = default)
        {
            while (!ct.IsCancellationRequested)
            {
                var clientSocket = await GetContextAsync(ct);
                if (ct.IsCancellationRequested || clientSocket == null)
                    return;

                // Spawn off each client task asynchronously
                var handler = new HttpHandler(clientSocket, this);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                handler.HandleClientRequest(ct);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        public void Dispose()
        {
            // do nothing
        }

        public TestHttpClient GetClient(string url = "/") => new TestHttpClient(this);

        private async Task<HttpListenerContext> GetContextAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_entryQueue.TryDequeue(out var entry)) return entry;

                await Task.Delay(100, ct);
            }

            return null;
        }

        public class TestHttpClient
        {
            public TestHttpClient(TestWebServer server)
            {
                Server = server;
            }

            public HttpListenerContext Context { get; set; }

            public TestWebServer Server { get; set; }

            public void GetResponse()
            {
                Server._entryQueue.Enqueue(Context);
            }
        }
    }
}

namespace Unosquare.Labs.EmbedIO.Tests.Mocks
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;

    internal class TestWebServer : IWebServer
    {
        private readonly ConcurrentQueue<IHttpContext> _entryQueue = new ConcurrentQueue<IHttpContext>();

        private readonly WebModules _modules = new WebModules();

        public TestWebServer(RoutingStrategy routingStrategy = RoutingStrategy.Wildcard)
        {
            RoutingStrategy = routingStrategy;
        }

        public ISessionWebModule SessionModule => _modules.SessionModule;
        public RoutingStrategy RoutingStrategy { get; }

        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();
        public Func<IHttpContext, Task<bool>> OnMethodNotAllowed { get; set; }
        public Func<IHttpContext, Task<bool>> OnNotFound { get; set; }

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
                
                // Usually we don't wait, but for testing let's do it.
                var handler = new HttpHandler(clientSocket);
                await handler.HandleClientRequest(ct);
            }
        }

        public void Dispose()
        {
            // do nothing
        }

        public TestHttpClient GetClient(string url = "/") => new TestHttpClient(this);

        private async Task<IHttpContext> GetContextAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                if (_entryQueue.TryDequeue(out var entry)) return entry;

                await Task.Delay(100, ct);
            }

            return null;
        }

        public class TestHttpClient : IHttpContext
        {
            public TestHttpClient(IWebServer server)
            {
                WebServer = server;
            }

            public IHttpRequest Request { get; private set; }
            public IHttpResponse Response { get; private set; }
            public IWebServer WebServer { get; set; }

            public async Task<string> GetAsync(string url)
            {
                Request = new TestHttpRequest(url);
                Response = new TestHttpResponse();

                if (!(WebServer is TestWebServer testServer))
                    throw new InvalidOperationException();

                testServer._entryQueue.Enqueue(this);

                if (!(Response.OutputStream is MemoryStream ms))
                    throw new InvalidOperationException();
                
                while (ms.Length == 0)
                    await Task.Delay(100);

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public class TestHttpRequest : IHttpRequest
        {
            public TestHttpRequest(string url)
            {
                Url = new Uri(url);
            }

            public NameValueCollection Headers { get; }
            public Version ProtocolVersion { get; }
            public bool KeepAlive { get; }
            public ICookieCollection Cookies { get; }
            public string RawUrl { get; }
            public NameValueCollection QueryString { get; }
            public string HttpMethod { get; } = HttpVerbs.Get.ToString();
            public Uri Url { get; }
            public bool HasEntityBody { get; }
            public Stream InputStream { get; }
            public Encoding ContentEncoding { get; }
            public IPEndPoint RemoteEndPoint { get; }
            public bool IsLocal { get; }
            public string UserAgent { get; }
            public bool IsWebSocketRequest { get; }
            public IPEndPoint LocalEndPoint { get; }
            public string ContentType { get; }
            public long ContentLength64 { get; }
            public bool IsAuthenticated { get; }
            public Uri UrlReferrer { get; }
        }

        public class TestHttpResponse : IHttpResponse
        {
            public NameValueCollection Headers { get; }
            public int StatusCode { get; set; }
            public long ContentLength64 { get; set; }
            public string ContentType { get; set; }
            public Stream OutputStream { get; } = new MemoryStream();
            public ICookieCollection Cookies { get; }
            public Encoding ContentEncoding { get; set; }
            public bool KeepAlive { get; set; }
            public Version ProtocolVersion { get; set; }
            public void AddHeader(string headerName, string value)
            {
                throw new NotImplementedException();
            }

            public void SetCookie(Cookie sessionCookie)
            {
                throw new NotImplementedException();
            }
        }
    }
}

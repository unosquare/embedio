using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Authentication;
using EmbedIO.Internal;
using EmbedIO.Routing;
using EmbedIO.Sessions;
using EmbedIO.Utilities;
using EmbedIO.WebSockets;
using Swan.Logging;

namespace EmbedIO.Testing.Internal
{
    internal sealed class TestContext : IHttpContextImpl
    {
        private readonly TimeKeeper _ageKeeper = new TimeKeeper();

        private readonly Stack<Action<IHttpContext>> _closeCallbacks = new Stack<Action<IHttpContext>>();

        private bool _closed;

        internal TestContext(IHttpRequest request)
        {
            Request = request;
            User = Auth.NoUser;
            TestResponse = new TestResponse();
            Id = UniqueIdGenerator.GetNext();
            LocalEndPoint = Request.LocalEndPoint;
            RemoteEndPoint = Request.RemoteEndPoint;
            Route = RouteMatch.None;
            Session = SessionProxy.None;
        }

        public string Id { get; }

        public CancellationToken CancellationToken { get; set; }

        public long Age => _ageKeeper.ElapsedTime;

        public IPEndPoint LocalEndPoint { get; }

        public IPEndPoint RemoteEndPoint { get; }

        public IHttpRequest Request { get; }

        public RouteMatch Route { get; set; }

        public string RequestedPath => Route.SubPath ?? string.Empty; // It will never be empty, because modules are matched via base routes - this is just to silence a warning.

        public IHttpResponse Response => TestResponse;

        internal TestResponse TestResponse { get; }

        public IPrincipal User { get; set; }

        public ISessionProxy Session { get; set; }

        public bool SupportCompressedRequests { get; set; }

        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        public bool IsHandled { get; set; }

        public MimeTypeProviderStack MimeTypeProviders { get; } = new MimeTypeProviderStack();

        public void SetHandled() => IsHandled = true;

        public void OnClose(Action<IHttpContext> callback)
        {
            if (_closed)
                throw new InvalidOperationException("HTTP context has already been closed.");

            _closeCallbacks.Push(Validate.NotNull(nameof(callback), callback));
        }

        public Task<IWebSocketContext> AcceptWebSocketAsync(IEnumerable<string> requestedProtocols,
                                                                  string acceptedProtocol,
                                                                  int receiveBufferSize,
                                                                  TimeSpan keepAliveInterval,
                                                                  CancellationToken cancellationToken)
            => throw new NotImplementedException("This HTTP context does not support the WebSocket protocol.");

        public void Close()
        {
            _closed = true;

            // Always close the response stream no matter what.
            Response.Close();

            foreach (var callback in _closeCallbacks)
            {
                try
                {
                    callback(this);
                }
                catch (Exception e)
                {
                    e.Log("HTTP context", "[Id] Exception thrown by a HTTP context close callback.");
                }
            }
        }

        public string GetMimeType(string extension)
            => MimeTypeProviders.GetMimeType(extension);

        public bool TryDetermineCompression(string mimeType, out bool preferCompression)
            => MimeTypeProviders.TryDetermineCompression(mimeType, out preferCompression);
    }
}
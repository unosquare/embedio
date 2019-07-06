using EmbedIO.Internal;
using EmbedIO.Sessions;
using EmbedIO.Utilities;
using EmbedIO.WebSockets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan;

namespace EmbedIO.Testing.Internal
{
    internal sealed class TestContext : IHttpContextImpl
    {
        private readonly TimeKeeper _ageKeeper = new TimeKeeper();

        private readonly Stack<Action<IHttpContext>> _closeCallbacks = new Stack<Action<IHttpContext>>();

        private bool _closed;

        public TestContext(IHttpRequest request)
        {
            Request = request;
            User = null;
            Response = new TestResponse();
            Id = UniqueIdGenerator.GetNext();
            LocalEndPoint = Request.LocalEndPoint;
            RemoteEndPoint = Request.RemoteEndPoint;
        }

        public string Id { get; }

        public long Age => _ageKeeper.ElapsedTime;

        public IPEndPoint LocalEndPoint { get; }

        public IPEndPoint RemoteEndPoint { get; }

        public IHttpRequest Request { get; }

        public IHttpResponse Response { get; }

        public IPrincipal User { get; }

        public ISessionProxy Session { get; set; }

        public bool SupportCompressedRequests { get; set; }

        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        public MimeTypeProviderStack MimeTypeProviders { get; } = new MimeTypeProviderStack();

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
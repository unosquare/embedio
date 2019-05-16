using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
using EmbedIO.Tests;
using EmbedIO.Tests.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Internal
{
    /// <summary>
    /// Represents a wrapper around a regular HttpListenerContext.
    /// </summary>
    /// <seealso cref="IHttpContext" />
    internal sealed class HttpContextImpl : IHttpContextImpl
    {
        private readonly HttpListenerContext _context;

        private readonly Stack<Action<IHttpContext>> _closeCallbacks;

        private bool _closed;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextImpl" /> class.
        /// </summary>
        /// <param name="context">The HTTP listener context.</param>
        public HttpContextImpl(HttpListenerContext context)
        {
            _context = context;

            Id = _context.Request.RequestTraceIdentifier.ToString("D", CultureInfo.InvariantCulture);
            LocalEndPoint = _context.Request.LocalEndPoint;
            RemoteEndPoint = _context.Request.RemoteEndPoint;
            Request = new HttpRequest(_context);
            User = _context.User;
            Response = new HttpResponse(_context);
        }

        public HttpContextImpl(TestHttpRequest request)
        {
            _context = null;

            Id = request.RequestTraceIdentifier.ToString("D", CultureInfo.InvariantCulture);
            LocalEndPoint = request.LocalEndPoint;
            RemoteEndPoint = request.RemoteEndPoint;
            Request = request;
            User = null;
            Response = new TestHttpResponse();
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public IPEndPoint LocalEndPoint { get; }

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public ISessionProxy Session { get; set; }

        /// <inheritdoc />
        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        /// <inheritdoc />
        public void OnClose(Action<IHttpContext> callback)
        {
            if (_closed)
                throw new InvalidOperationException("HTTP context has already been closed.");

            _closeCallbacks.Push(Validate.NotNull(nameof(callback), callback));
        }

        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
        {
            if (_context == null)
                throw new NotImplementedException();

            return new WebSocketContext(this, await _context.AcceptWebSocketAsync(subProtocol, receiveBufferSize, keepAliveInterval).ConfigureAwait(false));
        }

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
                    e.Log($"HTTP context", $"[Id] Exception thrown by a HTTP context close callback.");
                }
            }
        }
    }
}
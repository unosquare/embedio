using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

namespace EmbedIO.Internal
{
    /// <summary>
    /// Represents a wrapper around a regular HttpListenerContext.
    /// </summary>
    /// <seealso cref="IHttpContext" />
    internal class HttpContextImpl : IHttpContext
    {
        private readonly HttpListenerContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextImpl" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public HttpContextImpl(HttpListenerContext context)
        {
            _context = context;

            Id = _context.Request.RequestTraceIdentifier.ToString("D", CultureInfo.InvariantCulture);
            Request = new HttpRequest(_context);
            User = _context.User;
            Response = new HttpResponse(_context);
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        /// <inheritdoc />
        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval)
            => new WebSocketContext(await _context.AcceptWebSocketAsync(subProtocol, receiveBufferSize,keepAliveInterval).ConfigureAwait(false));
    }
}
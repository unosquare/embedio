#if !NETSTANDARD1_3
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Principal;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a wrapper around a regular HttpListenerContext.
    /// </summary>
    /// <seealso cref="IHttpContext" />
    public class HttpContext : IHttpContext
    {
        private readonly HttpListenerContext _context;
        private Lazy<IDictionary<object, object>> _items =
            new Lazy<IDictionary<object, object>>(() => new Dictionary<object, object>(), true);

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public HttpContext(HttpListenerContext context)
        {
            _context = context;
            Request = new HttpRequest(_context);
            User = _context.User;
            Response = new HttpResponse(_context);
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        /// <inheritdoc />
        public IDictionary<object, object> Items
        {
            get => _items.Value;
            set => _items = new Lazy<IDictionary<object, object>>(() => value, true);
        }

        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize)
            => new WebSocketContext(await _context.AcceptWebSocketAsync(subProtocol: null,
                receiveBufferSize: receiveBufferSize,
                keepAliveInterval: TimeSpan.FromSeconds(30))
                .ConfigureAwait(false));
    }
}
#endif
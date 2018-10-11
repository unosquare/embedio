#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System.Net;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a wrapper around a regular HttpListenerContext.
    /// </summary>
    /// <seealso cref="IHttpContext" />
    public class HttpContext : IHttpContext
    {
        private readonly HttpListenerContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public HttpContext(HttpListenerContext context)
        {
            _context = context;
            Request = new HttpRequest(_context);
            Response = new HttpResponse(_context);
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        /// <inheritdoc />
        public async Task<IWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize)
            => new WebSocketContext(await _context.AcceptWebSocketAsync(subProtocol: null,
                receiveBufferSize: receiveBufferSize,
                keepAliveInterval: TimeSpan.FromSeconds(30)));
    }
}
#endif
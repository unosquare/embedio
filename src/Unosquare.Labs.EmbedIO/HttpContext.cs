#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System.Net;
    using System;
    using System.Net.WebSockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a wrapper around a regular HttpListenerContext.
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.IHttpContext" />
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

        /// <summary>
        /// Accepts the web socket asynchronous.
        /// </summary>
        /// <param name="receiveBufferSize">Size of the receive buffer.</param>
        /// <returns>The WebSocketContext.</returns>
        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize)
            => _context.AcceptWebSocketAsync(subProtocol: null,
                receiveBufferSize: receiveBufferSize,
                keepAliveInterval: TimeSpan.FromSeconds(30));
    }
}
#endif
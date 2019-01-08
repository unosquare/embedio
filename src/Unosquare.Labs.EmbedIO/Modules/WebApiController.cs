namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Security.Principal;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// Inherit from this class and define your own Web API methods
    /// You must RegisterController in the Web API Module to make it active.
    /// </summary>
    public abstract class WebApiController : IHttpContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiController"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        protected WebApiController(IHttpContext context)
        {
            Request = context.Request;
            Response = context.Response;
            User = context.User;
            WebServer = context.WebServer;
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; internal set; }

        /// <inheritdoc />
        public IHttpResponse Response { get; internal set; }

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public IWebServer WebServer { get; set; }

        /// <inheritdoc />
        public Task<IWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the default headers to the Web API response.
        /// By default will set:
        ///
        /// Expires - Mon, 26 Jul 1997 05:00:00 GMT
        /// LastModified - (Current Date)
        /// CacheControl - no-store, no-cache, must-revalidate
        /// Pragma - no-cache
        ///
        /// Previous values are defined to avoid caching from client.
        /// </summary>
        public virtual void SetDefaultHeaders() => this.NoCache();
    }
}
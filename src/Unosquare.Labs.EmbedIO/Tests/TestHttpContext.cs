namespace Unosquare.Labs.EmbedIO.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Security.Principal;

    /// <summary>
    /// Represents a Test Http Context.
    /// </summary>
    /// <seealso cref="IHttpContext" />
    public class TestHttpContext : IHttpContext
    {
        private Lazy<IDictionary<object, object>> _items =
            new Lazy<IDictionary<object, object>>(() => new Dictionary<object, object>(), true);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpContext"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="webserver">The webserver.</param>
        public TestHttpContext(IHttpRequest request, IWebServer webserver)
        {
            Request = request;
            WebServer = webserver;
        }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; } = new TestHttpResponse();

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

        /// <exception cref="NotImplementedException"></exception>
        /// <inheritdoc />
        public Task<IWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize) => throw new NotImplementedException();
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Principal;
using EmbedIO.Utilities;

namespace EmbedIO.Tests.Internal
{
    /// <summary>
    /// Represents a Test Http Context.
    /// </summary>
    /// <seealso cref="IHttpContext" />
    internal class TestHttpContext : IHttpContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestHttpContext"/> class.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="webserver">The webserver.</param>
        public TestHttpContext(IHttpRequest request)
        {
            Id = UniqueIdGenerator.GetNext();
            Request = request;
        }

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public IHttpRequest Request { get; }

        /// <inheritdoc />
        public IHttpResponse Response { get; } = new TestHttpResponse();

        /// <inheritdoc />
        public IPrincipal User { get; }

        /// <inheritdoc />
        public IDictionary<object, object> Items { get; } = new Dictionary<object, object>();

        /// <inheritdoc />
        public Task<IWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval) 
            => throw new NotImplementedException();
    }
}
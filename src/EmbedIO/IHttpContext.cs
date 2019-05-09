using System;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Collections.Generic;

namespace EmbedIO
{
    /// <summary>
    /// Interface to create a HTTP Context.
    /// </summary>
    public interface IHttpContext
    {
        /// <summary>
        /// Gets a unique identifier for a HTTP context.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        string Id { get; }

        /// <summary>
        /// Gets the HTTP Request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        IHttpRequest Request { get; }

        /// <summary>
        /// Gets the HTTP Response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        IHttpResponse Response { get; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        IPrincipal User { get; }

        /// <summary>
        /// Gets or sets the dictionary of data to pass trough the EmbedIO pipeline.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        IDictionary<object, object> Items { get; }

        /// <summary>
        /// Accepts the web socket asynchronous.
        /// </summary>
        /// <param name="subProtocol">The sub-protocol.</param>
        /// <param name="receiveBufferSize">Size of the receive buffer.</param>
        /// <param name="keepAliveInterval">The keep alive interval.</param>
        /// <returns>
        /// A <see cref="IWebSocketContext" /> that represents
        /// the WebSocket handshake request.
        /// </returns>
        Task<IWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, TimeSpan keepAliveInterval);
    }
}
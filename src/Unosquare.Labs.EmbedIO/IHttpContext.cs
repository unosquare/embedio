﻿namespace Unosquare.Labs.EmbedIO
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to create a HTTP Context.
    /// </summary>
    public interface IHttpContext
    {
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
        /// Gets or sets the web server.
        /// </summary>
        /// <value>
        /// The web server.
        /// </value>
        IWebServer WebServer { get; set; }

        /// <summary>
        /// Accepts the web socket asynchronous.
        /// </summary>
        /// <param name="receiveBufferSize">Size of the receive buffer.</param>
        /// <returns>
        /// A <see cref="IWebSocketContext" /> that represents
        /// the WebSocket handshake request.
        /// </returns>
        Task<IWebSocketContext> AcceptWebSocketAsync(int receiveBufferSize);
    }
}

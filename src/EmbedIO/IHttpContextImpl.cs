﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// <para>Represents a HTTP context implementation, i.e. a HTTP context as seen internally by EmbedIO.</para>
    /// <para>This interface is only meant to be consumed internally by EmbedIO.</para>
    /// </summary>
    /// <seealso cref="IHttpContext" />
    public interface IHttpContextImpl : IHttpContext
    {
        /// <summary>
        /// Gets or sets the session proxy associated with this context.
        /// </summary>
        /// <value>
        /// A <see cref="ISessionProxy"/> interface.
        /// </value>
        new ISessionProxy Session { get; set; }

        /// <summary>
        /// Flushes and closes the response stream, then calls any registered close callbacks.
        /// </summary>
        /// <seealso cref="IHttpContext.OnClose"/>
        void Close();

        /// <summary>
        /// Asynchronously handles the HTTP part of a WebSocket request
        /// and returns a newly-created <seealso cref="IWebSocketContext"/> interface.
        /// </summary>
        /// <param name="requestedProtocols">The requested WebSocket sub-protocols.</param>
        /// <param name="acceptedProtocol">The accepted WebSocket sub-protocol.</param>
        /// <param name="receiveBufferSize">Size of the receive buffer.</param>
        /// <param name="keepAliveInterval">The keep-alive interval.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the server.</param>
        /// <returns>
        /// A <see cref="IWebSocketContext"/> interface.
        /// </returns>
        Task<IWebSocketContext> AcceptWebSocketAsync(
            IEnumerable<string> requestedProtocols, 
            string acceptedProtocol, 
            int receiveBufferSize, 
            TimeSpan keepAliveInterval,
            CancellationToken cancellationToken);
    }
}
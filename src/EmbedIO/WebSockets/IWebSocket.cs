using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.WebSockets
{
    /// <inheritdoc />
    /// <summary>
    /// Interface to create a WebSocket implementation.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public interface IWebSocket : IDisposable
    {
        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        WebSocketState State { get; }

        /// <summary>
        /// Sends the buffer to the web socket asynchronously.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="isText">if set to <c>true</c> [is text].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous of send data using websocket.
        /// </returns>
        Task SendAsync(byte[] buffer, bool isText, CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes the web socket asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The task object representing the asynchronous operation.
        /// </returns>
        Task CloseAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes the web socket asynchronously.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The task object representing the asynchronous operation.
        /// </returns>
        Task CloseAsync(CloseStatusCode code, string? comment = null, CancellationToken cancellationToken = default);
    }
}

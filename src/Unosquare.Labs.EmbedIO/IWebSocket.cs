namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Threading;
    using Net;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// Interface to create a WebSocket.
    /// </summary>
    /// <seealso cref="T:System.IDisposable" />
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
        /// Sends the asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="isText">if set to <c>true</c> [is text].</param>
        /// <param name="ct">The ct.</param>
        /// <returns>
        /// A task that represents the asynchronous of send data using websocket.
        /// </returns>
        Task SendAsync(byte[] buffer, bool isText, CancellationToken ct = default);

        /// <summary>
        /// Closes the asynchronous.
        /// </summary>
        /// <param name="ct">The ct.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task CloseAsync(CancellationToken ct = default);
    }
}

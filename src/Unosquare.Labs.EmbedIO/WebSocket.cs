﻿#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System.Threading;
    using System;
    using System.Net.WebSockets;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a wrapper around a regular WebSocketContext.
    /// </summary>
    /// <inheritdoc />
    public class WebSocket : IWebSocket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocket"/> class.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        public WebSocket(System.Net.WebSockets.WebSocket webSocket)
        {
            SystemWebSocket = webSocket;
        }

        /// <summary>
        /// Gets the real WebSocket object from System.Net.
        /// </summary>
        /// <value>
        /// The system web socket.
        /// </value>
        public System.Net.WebSockets.WebSocket SystemWebSocket { get; }

        /// <inheritdoc />
        public Net.WebSocketState State
        {
            get
            {
                switch (SystemWebSocket.State)
                {
                    case WebSocketState.Connecting:
                        return Net.WebSocketState.Connecting;
                    case WebSocketState.Open:
                        return Net.WebSocketState.Open;
                    default:
                        return Net.WebSocketState.Closed;
                }
            }
        }

        /// <inheritdoc />
        void IDisposable.Dispose() => SystemWebSocket?.Dispose();

        /// <inheritdoc />
        public Task SendAsync(byte[] buffer, bool isText, CancellationToken ct)
            => SystemWebSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                true,
                ct);

        /// <inheritdoc />
        public Task CloseAsync(CancellationToken ct) =>
            SystemWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
    }
}
#endif
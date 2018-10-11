#if !NETSTANDARD1_3 && !UWP
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
        private readonly System.Net.WebSockets.WebSocket _webSocket;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocket"/> class.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        public WebSocket(System.Net.WebSockets.WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        /// <inheritdoc />
        public Net.WebSocketState State
        {
            get
            {
                switch (_webSocket.State)
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
        public void Dispose() => _webSocket?.Dispose();

        /// <inheritdoc />
        public Task SendAsync(byte[] buffer, bool isText, CancellationToken ct)
            => _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                true,
                ct);

        /// <inheritdoc />
        public Task CloseAsync(bool isNormal, CancellationToken ct) =>
            _webSocket.CloseAsync(
                isNormal ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.MessageTooBig,
                isNormal ? string.Empty : $"Message too big. Maximum is XXX bytes.", // TODO: Complete
                ct);
    }
}
#endif
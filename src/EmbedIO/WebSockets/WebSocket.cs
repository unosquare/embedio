using System.Threading;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace EmbedIO
{
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
        public Task SendAsync(byte[] buffer, bool isText, CancellationToken cancellationToken = default)
            => SystemWebSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                true,
                cancellationToken);

        /// <inheritdoc />
        public Task CloseAsync(CancellationToken cancellationToken = default) =>
            SystemWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);

        /// <inheritdoc />
        public Task CloseAsync(Net.CloseStatusCode code, string comment = null, CancellationToken cancellationToken = default)=>
            SystemWebSocket.CloseAsync(MapCloseStatus(code), comment ?? string.Empty, cancellationToken);

        private WebSocketCloseStatus MapCloseStatus(Net.CloseStatusCode code)
        {
            switch (code)
            {
                case Net.CloseStatusCode.Normal:
                    return WebSocketCloseStatus.NormalClosure;
                case Net.CloseStatusCode.ProtocolError:
                    return WebSocketCloseStatus.ProtocolError;
                case Net.CloseStatusCode.InvalidData:
                case Net.CloseStatusCode.UnsupportedData:
                    return WebSocketCloseStatus.InvalidPayloadData;
                case Net.CloseStatusCode.PolicyViolation:
                    return WebSocketCloseStatus.PolicyViolation;
                case Net.CloseStatusCode.TooBig:
                    return WebSocketCloseStatus.MessageTooBig;
                case Net.CloseStatusCode.MandatoryExtension:
                    return WebSocketCloseStatus.MandatoryExtension;
                case Net.CloseStatusCode.ServerError:
                    return WebSocketCloseStatus.InternalServerError;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }
    }
}
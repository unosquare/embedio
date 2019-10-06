using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.WebSockets.Internal
{
    internal sealed class SystemWebSocket : IWebSocket
    {
        public SystemWebSocket(System.Net.WebSockets.WebSocket webSocket)
        {
            UnderlyingWebSocket = webSocket;
        }

        ~SystemWebSocket()
        {
            Dispose(false);
        }

        public System.Net.WebSockets.WebSocket UnderlyingWebSocket { get; }

        public WebSocketState State => UnderlyingWebSocket.State;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public Task SendAsync(byte[] buffer, bool isText, CancellationToken cancellationToken = default)
            => UnderlyingWebSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                true,
                cancellationToken);

        /// <inheritdoc />
        public Task CloseAsync(CancellationToken cancellationToken = default) =>
            UnderlyingWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);

        /// <inheritdoc />
        public Task CloseAsync(CloseStatusCode code, string? comment = null, CancellationToken cancellationToken = default)=>
            UnderlyingWebSocket.CloseAsync(MapCloseStatus(code), comment ?? string.Empty, cancellationToken);

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UnderlyingWebSocket.Dispose();
        }

        private WebSocketCloseStatus MapCloseStatus(CloseStatusCode code) => code switch {
            CloseStatusCode.Normal => WebSocketCloseStatus.NormalClosure,
            CloseStatusCode.ProtocolError => WebSocketCloseStatus.ProtocolError,
            CloseStatusCode.InvalidData => WebSocketCloseStatus.InvalidPayloadData,
            CloseStatusCode.UnsupportedData => WebSocketCloseStatus.InvalidPayloadData,
            CloseStatusCode.PolicyViolation => WebSocketCloseStatus.PolicyViolation,
            CloseStatusCode.TooBig => WebSocketCloseStatus.MessageTooBig,
            CloseStatusCode.MandatoryExtension => WebSocketCloseStatus.MandatoryExtension,
            CloseStatusCode.ServerError => WebSocketCloseStatus.InternalServerError,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
        };
    }
}
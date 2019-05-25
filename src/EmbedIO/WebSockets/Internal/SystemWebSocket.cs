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

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UnderlyingWebSocket.Dispose();
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
        public Task CloseAsync(CloseStatusCode code, string comment = null, CancellationToken cancellationToken = default)=>
            UnderlyingWebSocket.CloseAsync(MapCloseStatus(code), comment ?? string.Empty, cancellationToken);

        private WebSocketCloseStatus MapCloseStatus(CloseStatusCode code)
        {
            switch (code)
            {
                case CloseStatusCode.Normal:
                    return WebSocketCloseStatus.NormalClosure;
                case CloseStatusCode.ProtocolError:
                    return WebSocketCloseStatus.ProtocolError;
                case CloseStatusCode.InvalidData:
                case CloseStatusCode.UnsupportedData:
                    return WebSocketCloseStatus.InvalidPayloadData;
                case CloseStatusCode.PolicyViolation:
                    return WebSocketCloseStatus.PolicyViolation;
                case CloseStatusCode.TooBig:
                    return WebSocketCloseStatus.MessageTooBig;
                case CloseStatusCode.MandatoryExtension:
                    return WebSocketCloseStatus.MandatoryExtension;
                case CloseStatusCode.ServerError:
                    return WebSocketCloseStatus.InternalServerError;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }
    }
}
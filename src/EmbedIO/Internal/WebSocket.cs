using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Internal
{
    internal sealed class WebSocket : IWebSocket
    {
        public WebSocket(System.Net.WebSockets.WebSocket webSocket)
        {
            SystemWebSocket = webSocket;
        }

        ~WebSocket()
        {
            Dispose(false);
        }

        public System.Net.WebSockets.WebSocket SystemWebSocket { get; }

        public WebSocketState State => SystemWebSocket.State;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            SystemWebSocket.Dispose();
        }

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
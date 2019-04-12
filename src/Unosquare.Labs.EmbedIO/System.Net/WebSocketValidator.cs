namespace Unosquare.Net
{
    using Labs.EmbedIO.Constants;
    using Swan;
    using System.Text;

    internal class WebSocketValidator
    {
        private readonly WebSocket _webSocket;

        public WebSocketValidator(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        internal static bool CheckParametersForClose(CloseStatusCode code, string reason)
        {
            if (code == CloseStatusCode.NoStatus && !string.IsNullOrEmpty(reason))
            {
                "'code' cannot have a reason.".Trace(nameof(CheckParametersForClose));
                return false;
            }

            if (code == CloseStatusCode.MandatoryExtension)
            {
                "'code' cannot be used by a server.".Trace(nameof(CheckParametersForClose));
                return false;
            }

            if (!string.IsNullOrEmpty(reason) && Encoding.UTF8.GetBytes(reason).Length > 123)
            {
                "The size of 'reason' is greater than the allowable max size.".Trace(nameof(CheckParametersForClose));
                return false;
            }

            return true;
        }

        internal bool CheckIfAvailable(bool connecting = true)
        {
            if (connecting || _webSocket.State != WebSocketState.Connecting) return true;

            "This operation isn't available in: connecting".Trace(nameof(CheckIfAvailable));
            return false;
        }

        // As server
        internal void ThrowIfInvalid(WebSocketContext context)
        {
            if (context.RequestUri == null)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "Specifies an invalid Request-URI.");
            }

            if (!context.IsWebSocketRequest)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "Not a WebSocket handshake request.");
            }

            var headers = context.Headers;
            if (string.IsNullOrEmpty(headers[HttpHeaders.WebSocketKey]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes no {HttpHeaders.WebSocketKey} header, or it has an invalid value.");
            }

            if (!ValidateSecWebSocketVersionClientHeader(headers[HttpHeaders.WebSocketVersion]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes no {HttpHeaders.WebSocketVersion} header, or it has an invalid value.");
            }

            if (!ValidateSecWebSocketProtocolClientHeader(headers[HttpHeaders.WebSocketProtocol]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes an invalid {HttpHeaders.WebSocketProtocol} header.");
            }

            if (!_webSocket.IgnoreExtensions
                && !string.IsNullOrWhiteSpace(headers[HttpHeaders.WebSocketExtensions]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes an invalid {HttpHeaders.WebSocketExtensions} header.");
            }
        }

        private static bool ValidateSecWebSocketProtocolClientHeader(string value) => value == null || value.Length > 0;

        // As server
        private static bool ValidateSecWebSocketVersionClientHeader(string value) => value != null && value == Strings.WebSocketVersion;
    }
}
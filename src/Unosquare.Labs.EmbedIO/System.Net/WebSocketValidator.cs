namespace Unosquare.Net
{
    using System.Linq;
    using System.Text;
    using Labs.EmbedIO.Constants;
    using Swan;

    internal class WebSocketValidator
    {
        private readonly WebSocket _webSocket;

        public WebSocketValidator(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        internal static bool CheckParametersForClose(CloseStatusCode code, string reason, bool client)
        {
            if (code == CloseStatusCode.NoStatus && !string.IsNullOrEmpty(reason))
            {
                "'code' cannot have a reason.".Error();
                return false;
            }

            if (code == CloseStatusCode.MandatoryExtension && !client)
            {
                "'code' cannot be used by a server.".Error();
                return false;
            }

            if (code == CloseStatusCode.ServerError && client)
            {
                "'code' cannot be used by a client.".Error();
                return false;
            }

            if (!string.IsNullOrEmpty(reason) && Encoding.UTF8.GetBytes(reason).Length > 123)
            {
                "The size of 'reason' is greater than the allowable max size.".Error();
                return false;
            }

            return true;
        }

        internal void ThrowIfInvalidResponse(HttpResponse response)
        {
            if (response.IsRedirect)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "Indicates the redirection.");
            }

            if (response.IsUnauthorized)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "Requires the authentication.");
            }

            if (!response.IsWebSocketResponse)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "Not a WebSocket handshake response.");
            }

            var headers = response.Headers;
            if (headers[Headers.WebSocketAccept]?.TrimStart() != _webSocket.WebSocketKey.CreateResponseKey())
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes no {Headers.WebSocketAccept} header, or it has an invalid value.");
            }

            if (!ValidateSecWebSocketExtensionsServerHeader(headers[Headers.WebSocketExtensions]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes an invalid {Headers.WebSocketExtensions} header.");
            }

            if (!ValidateSecWebSocketVersionServerHeader(headers[Headers.WebSocketVersion]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes an invalid {Headers.WebSocketVersion} header.");
            }
        }

        internal bool CheckIfAvailable(bool connecting = true, bool open = true, bool closing = false, bool closed = false)
        {
            if (!connecting && _webSocket.State == WebSocketState.Connecting)
            {
                "This operation isn't available in: connecting".Error();
                return false;
            }

            if (!open && _webSocket.State == WebSocketState.Open)
            {
                "This operation isn't available in: open".Error();
                return false;
            }

            if (!closing && _webSocket.State == WebSocketState.Closing)
            {
                "This operation isn't available in: closing".Error();
                return false;
            }

            if (!closed && _webSocket.State == WebSocketState.Closed)
            {
                "This operation isn't available in: closed".Error();
                return false;
            }

            return true;
        }

        internal bool CheckIfAvailable(
            bool client,
            bool server,
            bool connecting,
            bool open,
            bool closing,
            bool closed = true)
        {
            if (!client && _webSocket.IsClient)
            {
                "This operation isn't available in: client".Error();
                return false;
            }

            if (!server && !_webSocket.IsClient)
            {
                "This operation isn't available in: server".Error();
                return false;
            }

            return CheckIfAvailable(connecting, open, closing, closed);
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
            if (string.IsNullOrEmpty(headers[Headers.WebSocketKey]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes no {Headers.WebSocketKey} header, or it has an invalid value.");
            }

            if (!ValidateSecWebSocketVersionClientHeader(headers[Headers.WebSocketVersion]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes no {Headers.WebSocketVersion} header, or it has an invalid value.");
            }

            if (!ValidateSecWebSocketProtocolClientHeader(headers[Headers.WebSocketProtocol]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes an invalid {Headers.WebSocketProtocol} header.");
            }

            if (!_webSocket.IgnoreExtensions
                && !string.IsNullOrWhiteSpace(headers[Headers.WebSocketExtensions]))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, $"Includes an invalid Headers.WebSocketExtensions header.");
            }
        }

        // As client
        internal bool ValidateSecWebSocketExtensionsServerHeader(string value)
        {
            if (value == null)
                return true;

            if (value.Length == 0 || !_webSocket.IsExtensionsRequested)
                return false;

            var comp = _webSocket.Compression != CompressionMethod.None;

            foreach (var e in value.SplitHeaderValue(Strings.CommaSplitChar))
            {
                var ext = e.Trim();
                if (!comp || !ext.IsCompressionExtension(_webSocket.Compression))
                    return false;

                if (!ext.Contains("server_no_context_takeover"))
                {
                    "The server hasn't sent back 'server_no_context_takeover'.".Error();
                    return false;
                }

                if (!ext.Contains("client_no_context_takeover"))
                    "The server hasn't sent back 'client_no_context_takeover'.".Info();

                var method = _webSocket.Compression.ToExtensionString();
                var invalid =
                    ext.SplitHeaderValue(';').Any(
                        t =>
                        {
                            t = t.Trim();
                            return t != method
                                   && t != "server_no_context_takeover"
                                   && t != "client_no_context_takeover";
                        });

                if (invalid)
                    return false;
            }

            return true;
        }

        private static bool ValidateSecWebSocketProtocolClientHeader(string value) => value == null || value.Length > 0;

        // As server
        private static bool ValidateSecWebSocketVersionClientHeader(string value) => value != null && value == Strings.WebSocketVersion;

        // As client
        private static bool ValidateSecWebSocketVersionServerHeader(string value) => value == null || value == Strings.WebSocketVersion;
    }
}
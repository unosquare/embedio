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

        internal static string CheckIfAvailable(
            WebSocketState state,
            bool connecting = false,
            bool open = true,
            bool closing = false,
            bool closed = false)
        {
            return (!connecting && state == WebSocketState.Connecting) ||
                   (!open && state == WebSocketState.Open) ||
                   (!closing && state == WebSocketState.Closing) ||
                   (!closed && state == WebSocketState.Closed)
                ? "This operation isn't available in: " + state.ToString().ToLower()
                : null;
        }
        
        internal static bool CheckParametersForClose(CloseStatusCode code, string reason, bool client = true)
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

        internal static string CheckPingParameter(string message, out byte[] bytes)
        {
            bytes = Encoding.UTF8.GetBytes(message);
            return bytes.Length > 125 ? "A message has greater than the allowable max size." : null;
        }

        internal static string CheckSendParameter(byte[] data) => data == null ? "'data' is null." : null;
        
        internal bool CheckHandshakeResponse(HttpResponse response, out string message)
        {
            message = null;

            if (response.IsRedirect)
            {
                message = "Indicates the redirection.";
                return false;
            }

            if (response.IsUnauthorized)
            {
                message = "Requires the authentication.";
                return false;
            }

            if (!response.IsWebSocketResponse)
            {
                message = "Not a WebSocket handshake response.";
                return false;
            }

            var headers = response.Headers;
            if (!_webSocket.ValidateSecWebSocketAcceptHeader(headers["Sec-WebSocket-Accept"]))
            {
                message = "Includes no Sec-WebSocket-Accept header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketExtensionsServerHeader(headers["Sec-WebSocket-Extensions"]))
            {
                message = "Includes an invalid Sec-WebSocket-Extensions header.";
                return false;
            }

            if (!ValidateSecWebSocketVersionServerHeader(headers["Sec-WebSocket-Version"]))
            {
                message = "Includes an invalid Sec-WebSocket-Version header.";
                return false;
            }

            return true;
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
        internal bool CheckHandshakeRequest(WebSocketContext context, out string message)
        {
            message = null;

            if (context.RequestUri == null)
            {
                message = "Specifies an invalid Request-URI.";
                return false;
            }

            if (!context.IsWebSocketRequest)
            {
                message = "Not a WebSocket handshake request.";
                return false;
            }

            var headers = context.Headers;
            if (!ValidateSecWebSocketKeyHeader(headers["Sec-WebSocket-Key"]))
            {
                message = "Includes no Sec-WebSocket-Key header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketVersionClientHeader(headers["Sec-WebSocket-Version"]))
            {
                message = "Includes no Sec-WebSocket-Version header, or it has an invalid value.";
                return false;
            }

            if (!ValidateSecWebSocketProtocolClientHeader(headers["Sec-WebSocket-Protocol"]))
            {
                message = "Includes an invalid Sec-WebSocket-Protocol header.";
                return false;
            }

            if (!_webSocket.IgnoreExtensions
                && !string.IsNullOrWhiteSpace(headers["Sec-WebSocket-Extensions"]))
            {
                message = "Includes an invalid Sec-WebSocket-Extensions header.";
                return false;
            }

            return true;
        }

        internal void CheckReceivedFrame(WebSocketFrame frame)
        {
            var masked = frame.IsMasked;

            if (_webSocket.IsClient && masked)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "A frame from the server is masked.");
            }

            if (!_webSocket.IsClient && !masked)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "A frame from a client isn't masked.");
            }

            if (_webSocket.InContinuation && frame.IsData)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "A data frame has been received while receiving continuation frames.");
            }

            if (frame.IsCompressed && _webSocket.Compression == CompressionMethod.None)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "A compressed frame has been received without any agreement for it.");
            }

            if (frame.Rsv2 == Rsv.On)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "The RSV2 of a frame is non-zero without any negotiation for it.");
            }

            if (frame.Rsv3 == Rsv.On)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "The RSV3 of a frame is non-zero without any negotiation for it.");
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
                if (comp && ext.IsCompressionExtension(_webSocket.Compression))
                {
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
                else
                {
                    return false;
                }
            }

            return true;
        }

        // As server
        private static bool ValidateSecWebSocketKeyHeader(string value) => !string.IsNullOrEmpty(value);

        private static bool ValidateSecWebSocketProtocolClientHeader(string value) => value == null || value.Length > 0;

        // As server
        private static bool ValidateSecWebSocketVersionClientHeader(string value) => value != null && value == WebSocket.Version;

        // As client
        private static bool ValidateSecWebSocketVersionServerHeader(string value) => value == null || value == WebSocket.Version;
    }
}
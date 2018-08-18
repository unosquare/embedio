#if !NET47
namespace Unosquare.Net
{
    using System;
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
        
        internal static bool CheckParametersForClose(CloseStatusCode code, string reason, bool client, out string message)
        {
            message = null;

            if (code == CloseStatusCode.NoStatus && !string.IsNullOrEmpty(reason))
            {
                message = "'code' cannot have a reason.";
                return false;
            }

            if (code == CloseStatusCode.MandatoryExtension && !client)
            {
                message = "'code' cannot be used by a server.";
                return false;
            }

            if (code == CloseStatusCode.ServerError && client)
            {
                message = "'code' cannot be used by a client.";
                return false;
            }

            if (!string.IsNullOrEmpty(reason) && Encoding.UTF8.GetBytes(reason).Length > 123)
            {
                message = "The size of 'reason' is greater than the allowable max size.";
                return false;
            }

            return true;
        }

        internal static bool CheckWaitTime(TimeSpan time, out string message)
        {
            message = null;

            if (time > TimeSpan.Zero) return true;

            message = "A wait time is zero or less.";
            return false;
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

        internal bool CheckIfAvailable(out string message, bool connecting = true, bool open = true, bool closing = false, bool closed = false)
        {
            message = null;

            if (!connecting && _webSocket.State == WebSocketState.Connecting)
            {
                message = "This operation isn't available in: connecting";
                return false;
            }

            if (!open && _webSocket.State == WebSocketState.Open)
            {
                message = "This operation isn't available in: open";
                return false;
            }

            if (!closing && _webSocket.State == WebSocketState.Closing)
            {
                message = "This operation isn't available in: closing";
                return false;
            }

            if (!closed && _webSocket.State == WebSocketState.Closed)
            {
                message = "This operation isn't available in: closed";
                return false;
            }

            return true;
        }

        internal bool CheckIfAvailable(
            out string message,
            bool client,
            bool server,
            bool connecting,
            bool open,
            bool closing,
            bool closed = true)
        {
            if (!client && _webSocket.IsClient)
            {
                message = "This operation isn't available in: client";
                return false;
            }

            if (!server && !_webSocket.IsClient)
            {
                message = "This operation isn't available in: server";
                return false;
            }

            return CheckIfAvailable(out message, connecting, open, closing, closed);
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
#endif
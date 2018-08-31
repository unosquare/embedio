namespace Unosquare.Net
{
    using System;

    /// <summary>
    /// The exception that is thrown when a <see cref="WebSocket"/> gets a fatal error.
    /// </summary>
    public class WebSocketException : Exception
    {
        internal WebSocketException(string message, Exception innerException = null)
            : this(CloseStatusCode.Abnormal, message, innerException)
        {
        }

        internal WebSocketException(CloseStatusCode code, Exception innerException = null)
            : this(code, null, innerException)
        {
        }

        internal WebSocketException(CloseStatusCode code, string message, Exception innerException = null)
            : base(message ?? GetMessage(code), innerException)
        {
            Code = code;
        }

        /// <summary>
        /// Gets the status code indicating the cause of the exception.
        /// </summary>
        /// <value>
        /// One of the <see cref="CloseStatusCode"/> enum values, represents the status code
        /// indicating the cause of the exception.
        /// </value>
        public CloseStatusCode Code { get; }

        internal static string GetMessage(CloseStatusCode code)
        {
            switch (code)
            {
                case CloseStatusCode.ProtocolError:
                    return "A WebSocket protocol error has occurred.";
                case CloseStatusCode.UnsupportedData:
                    return "Unsupported data has been received.";
                case CloseStatusCode.Abnormal:
                    return "An exception has occurred.";
                case CloseStatusCode.InvalidData:
                    return "Invalid data has been received.";
                case CloseStatusCode.PolicyViolation:
                    return "A policy violation has occurred.";
                case CloseStatusCode.TooBig:
                    return "A too big message has been received.";
                case CloseStatusCode.MandatoryExtension:
                    return "WebSocket client didn't receive expected extension(s).";
                case CloseStatusCode.ServerError:
                    return "WebSocket server got an internal error.";
                case CloseStatusCode.TlsHandshakeFailure:
                    return "An error has occurred during a TLS handshake.";
                default:
                    return string.Empty;
            }
        }
    }
}
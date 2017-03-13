#if !NET46
#region License
/*
 * WebSocketException.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2014 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

using System;

namespace Unosquare.Net
{
    /// <summary>
    /// The exception that is thrown when a <see cref="WebSocket"/> gets a fatal error.
    /// </summary>
    public class WebSocketException : Exception
    {
        #region Internal Constructors

        internal WebSocketException(Exception innerException = null)
            : this(CloseStatusCode.Abnormal, null, innerException)
        {
        }

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

        internal static string GetMessage(CloseStatusCode code)
        {
            return code == CloseStatusCode.ProtocolError
                ? "A WebSocket protocol error has occurred."
                : code == CloseStatusCode.UnsupportedData
                    ? "Unsupported data has been received."
                    : code == CloseStatusCode.Abnormal
                        ? "An exception has occurred."
                        : code == CloseStatusCode.InvalidData
                            ? "Invalid data has been received."
                            : code == CloseStatusCode.PolicyViolation
                                ? "A policy violation has occurred."
                                : code == CloseStatusCode.TooBig
                                    ? "A too big message has been received."
                                    : code == CloseStatusCode.MandatoryExtension
                                        ? "WebSocket client didn't receive expected extension(s)."
                                        : code == CloseStatusCode.ServerError
                                            ? "WebSocket server got an internal error."
                                            : code == CloseStatusCode.TlsHandshakeFailure
                                                ? "An error has occurred during a TLS handshake."
                                                : string.Empty;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the status code indicating the cause of the exception.
        /// </summary>
        /// <value>
        /// One of the <see cref="CloseStatusCode"/> enum values, represents the status code
        /// indicating the cause of the exception.
        /// </value>
        public CloseStatusCode Code { get; }

        #endregion
    }
}

#endif
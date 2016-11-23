#if !NET46
//------------------------------------------------------------------------------
// <copyright file="WebSocketReceiveResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Unosquare.Net
{
    /// <summary>
    /// Enums WS Message Type
    /// </summary>
    public enum WebSocketMessageType
    {
        /// <summary>
        /// The text
        /// </summary>
        Text = 0,
        /// <summary>
        /// The binary
        /// </summary>
        Binary = 1,
        /// <summary>
        /// The close
        /// </summary>
        Close = 2
    }

    /// <summary>
    /// Enums WS Close Status
    /// </summary>
    public enum WebSocketCloseStatus
    {
        NormalClosure = 1000,
        EndpointUnavailable = 1001,
        ProtocolError = 1002,
        InvalidMessageType = 1003,
        Empty = 1005,
        // AbnormalClosure = 1006, // 1006 is reserved and should never be used by user
        InvalidPayloadData = 1007,
        PolicyViolation = 1008,
        MessageTooBig = 1009,
        MandatoryExtension = 1010,
        InternalServerError = 1011
        // TLSHandshakeFailed = 1015, // 1015 is reserved and should never be used by user

        // 0 - 999 Status codes in the range 0-999 are not used.
        // 1000 - 1999 Status codes in the range 1000-1999 are reserved for definition by this protocol.
        // 2000 - 2999 Status codes in the range 2000-2999 are reserved for use by extensions.
        // 3000 - 3999 Status codes in the range 3000-3999 MAY be used by libraries and frameworks. The 
        //             interpretation of these codes is undefined by this protocol. End applications MUST 
        //             NOT use status codes in this range.       
        // 4000 - 4999 Status codes in the range 4000-4999 MAY be used by application code. The interpretaion
        //             of these codes is undefined by this protocol.
    }

    /// <summary>
    /// Represents a WS Receive result
    /// </summary>
    public class WebSocketReceiveResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketReceiveResult" /> class.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
                    : this(count, messageType, endOfMessage, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketReceiveResult" /> class.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="endOfMessage">if set to <c>true</c> [end of message].</param>
        /// <param name="closeStatus">The close status.</param>
        /// <param name="closeStatusDescription">The close status description.</param>
        public WebSocketReceiveResult(int count,
                    WebSocketMessageType messageType,
                    bool endOfMessage,
                    WebSocketCloseStatus? closeStatus,
                    string closeStatusDescription)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
            EndOfMessage = endOfMessage;
            MessageType = messageType;
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count { get; private set; }
        /// <summary>
        /// Gets a value indicating whether [end of message].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [end of message]; otherwise, <c>false</c>.
        /// </value>
        public bool EndOfMessage { get; }
        /// <summary>
        /// Gets the type of the message.
        /// </summary>
        /// <value>
        /// The type of the message.
        /// </value>
        public WebSocketMessageType MessageType { get; }
        /// <summary>
        /// Gets the close status.
        /// </summary>
        /// <value>
        /// The close status.
        /// </value>
        public WebSocketCloseStatus? CloseStatus { get; }
        /// <summary>
        /// Gets the close status description.
        /// </summary>
        /// <value>
        /// The close status description.
        /// </value>
        public string CloseStatusDescription { get; }
    }
}
#endif
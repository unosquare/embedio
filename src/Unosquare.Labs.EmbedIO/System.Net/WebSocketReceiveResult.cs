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
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
            EndOfMessage = endOfMessage;
            MessageType = messageType;
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
    }
}
#endif
#if !NET46
//------------------------------------------------------------------------------
// <copyright file="WebSocketReceiveResult.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

namespace Unosquare.Net
{
    public enum WebSocketMessageType
    {
        Text = 0,
        Binary = 1,
        Close = 2
    }

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

    public class WebSocketReceiveResult
    {
        public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
            : this(count, messageType, endOfMessage, null, null)
        {
        }

        public WebSocketReceiveResult(int count,
            WebSocketMessageType messageType,
            bool endOfMessage,
            Nullable<WebSocketCloseStatus> closeStatus,
            string closeStatusDescription)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            this.Count = count;
            this.EndOfMessage = endOfMessage;
            this.MessageType = messageType;
            this.CloseStatus = closeStatus;
            this.CloseStatusDescription = closeStatusDescription;
        }

        public int Count { get; private set; }
        public bool EndOfMessage { get; private set; }
        public WebSocketMessageType MessageType { get; private set; }
        public Nullable<WebSocketCloseStatus> CloseStatus { get; private set; }
        public string CloseStatusDescription { get; private set; }

        internal WebSocketReceiveResult Copy(int count)
        {
            this.Count -= count;
            return new WebSocketReceiveResult(count,
                this.MessageType,
                this.Count == 0 && this.EndOfMessage,
                this.CloseStatus,
                this.CloseStatusDescription);
        }
    }
}
#endif
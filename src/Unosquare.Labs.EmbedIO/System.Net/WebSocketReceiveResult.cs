﻿namespace Unosquare.Net
{
    using System;
    using Labs.EmbedIO;

    /// <summary>
    /// Represents a WS Receive result.
    /// </summary>
    public class WebSocketReceiveResult : IWebSocketReceiveResult
    {
        internal WebSocketReceiveResult(int count, Opcode code)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
            EndOfMessage = code == Opcode.Close;
            MessageType = code == Opcode.Text ? 0 : 1;
        }

        /// <inheritdoc />
        public int Count { get; }
        
        /// <inheritdoc />
        public bool EndOfMessage { get; }
        
        /// <inheritdoc />
        public int MessageType { get; }
    }
}
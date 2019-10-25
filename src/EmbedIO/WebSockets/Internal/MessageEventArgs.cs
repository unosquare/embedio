using System;
using Swan;

namespace EmbedIO.WebSockets.Internal
{
    /// <summary>
    /// Represents the event data for the <see cref="Internal.WebSocket.OnMessage"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   That event occurs when the <see cref="Internal.WebSocket"/> receives
    ///   a message or a ping if the <see cref="Internal.WebSocket.EmitOnPing"/>
    ///   property is set to <c>true</c>.
    ///   </para>
    ///   <para>
    ///   If you would like to get the message data, you should access
    ///   the <see cref="Data"/> or <see cref="RawData"/> property.
    ///   </para>
    /// </remarks>
    internal class MessageEventArgs : EventArgs
    {
        private readonly byte[] _rawData;
        private string? _data;
        private bool _dataSet;

        internal MessageEventArgs(WebSocketFrame frame)
        {
            Opcode = frame.Opcode;
            _rawData = frame.PayloadData.ApplicationData.ToArray();
        }

        internal MessageEventArgs(Opcode opcode, byte[] rawData)
        {
            if ((ulong)rawData.Length > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig);

            Opcode = opcode;
            _rawData = rawData;
        }

        /// <summary>
        /// Gets the message data as a <see cref="string"/>.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the message data if its type is
        /// text or ping and if decoding it to a string has successfully done;
        /// otherwise, <see langword="null"/>.
        /// </value>
        public string? Data
        {
            get
            {
                SetData();
                return _data;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the message type is binary.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is binary; otherwise, <c>false</c>.
        /// </value>
        public bool IsBinary => Opcode == Opcode.Binary;

        /// <summary>
        /// Gets a value indicating whether the message type is ping.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is ping; otherwise, <c>false</c>.
        /// </value>
        public bool IsPing => Opcode == Opcode.Ping;

        /// <summary>
        /// Gets a value indicating whether the message type is text.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is text; otherwise, <c>false</c>.
        /// </value>
        public bool IsText => Opcode == Opcode.Text;

        /// <summary>
        /// Gets the message data as an array of <see cref="byte"/>.
        /// </summary>
        /// <value>
        /// An array of <see cref="byte"/> that represents the message data.
        /// </value>
        public byte[] RawData
        {
            get
            {
                SetData();
                return _rawData;
            }
        }

        internal Opcode Opcode { get; }

        private void SetData()
        {
            if (_dataSet)
                return;

            if (Opcode == Opcode.Binary)
            {
                _dataSet = true;
                return;
            }

            _data = _rawData.ToText();
            _dataSet = true;
        }
    }
}
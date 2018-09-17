namespace Unosquare.Net
{
    using Labs.EmbedIO.Constants;
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Indicates whether a WebSocket frame is the final frame of a message.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">Section 5.2</see> of RFC 6455.
    /// </remarks>
    internal enum Fin : byte
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates more frames of a message follow.
        /// </summary>
        More = 0x0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates the final frame of a message.
        /// </summary>
        Final = 0x1,
    }

    /// <summary>
    /// Indicates whether the payload data of a WebSocket frame is masked.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">Section 5.2</see> of RFC 6455.
    /// </remarks>
    internal enum Mask : byte
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates not masked.
        /// </summary>
        Off = 0x0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates masked.
        /// </summary>
        On = 0x1,
    }

    /// <summary>
    /// Indicates whether each RSV (RSV1, RSV2, and RSV3) of a WebSocket frame is non-zero.
    /// </summary>
    /// <remarks>
    /// The values of this enumeration are defined in
    /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">Section 5.2</see> of RFC 6455.
    /// </remarks>
    internal enum Rsv : byte
    {
        /// <summary>
        /// Equivalent to numeric value 0. Indicates zero.
        /// </summary>
        Off = 0x0,

        /// <summary>
        /// Equivalent to numeric value 1. Indicates non-zero.
        /// </summary>
        On = 0x1,
    }

    internal class WebSocketFrame
    {
        internal static readonly byte[] EmptyPingBytes;
        
        static WebSocketFrame()
        {
            EmptyPingBytes = CreatePingFrame(false).ToArray();
        }

        internal WebSocketFrame(Opcode opcode, PayloadData payloadData, bool mask = true)
            : this(Fin.Final, opcode, payloadData, false, mask)
        {
        }

        internal WebSocketFrame(Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask = true)
            : this(fin, opcode, new PayloadData(data), compressed, mask)
        {
        }

        internal WebSocketFrame(
            Fin fin, Opcode opcode, PayloadData payloadData, bool compressed = false, bool mask = true)
        {
            Fin = fin;
            Rsv1 = IsOpcodeData(opcode) && compressed ? Rsv.On : Rsv.Off;
            Rsv2 = Rsv.Off;
            Rsv3 = Rsv.Off;
            Opcode = opcode;

            var len = payloadData.Length;
            if (len < 126)
            {
                PayloadLength = (byte)len;
                ExtendedPayloadLength = WebSocket.EmptyBytes;
            }
            else if (len < 0x010000)
            {
                PayloadLength = (byte)126;
                ExtendedPayloadLength = ((ushort)len).ToByteArray(Endianness.Big);
            }
            else
            {
                PayloadLength = (byte)127;
                ExtendedPayloadLength = len.ToByteArray(Endianness.Big);
            }

            if (mask)
            {
                Mask = Mask.On;
                MaskingKey = CreateMaskingKey();
                payloadData.Mask(MaskingKey);
            }
            else
            {
                Mask = Mask.Off;
                MaskingKey = WebSocket.EmptyBytes;
            }

            PayloadData = payloadData;
        }

        internal WebSocketFrame()
        {
        }

        public byte[] ExtendedPayloadLength { get; internal set; }

        public Fin Fin { get; internal set; }

        public bool IsCompressed => Rsv1 == Rsv.On;
        
        public bool IsFragment => Fin == Fin.More || Opcode == Opcode.Cont;

        public bool IsMasked => Mask == Mask.On;

        public Mask Mask { get; internal set; }

        public byte[] MaskingKey { get; internal set; }

        public Opcode Opcode { get; internal set; }

        public PayloadData PayloadData { get; internal set; }

        public byte PayloadLength { get; internal set; }

        public Rsv Rsv1 { get; internal set; }

        public Rsv Rsv2 { get; internal set; }

        public Rsv Rsv3 { get; internal set; }

        internal int ExtendedPayloadLengthCount => PayloadLength < 126 ? 0 : (PayloadLength == 126 ? 2 : 8);

        internal ulong FullPayloadLength => PayloadLength < 126
            ? PayloadLength
            : PayloadLength == 126
                ? BitConverter.ToUInt16(ExtendedPayloadLength.ToHostOrder(Endianness.Big), 0)
                : BitConverter.ToUInt64(ExtendedPayloadLength.ToHostOrder(Endianness.Big), 0);

        public IEnumerator<byte> GetEnumerator() => ((IEnumerable<byte>)ToArray()).GetEnumerator();

        public string PrintToString()
        {
            // Payload Length
            var payloadLen = PayloadLength;

            // Extended Payload Length
            var extPayloadLen = payloadLen > 125 ? FullPayloadLength.ToString() : string.Empty;

            // Masking Key
            var maskingKey = BitConverter.ToString(MaskingKey);

            // Payload Data
            var payload = payloadLen == 0
                ? string.Empty
                : payloadLen > 125
                    ? "---"
                    : Opcode == Opcode.Text && !(IsFragment || IsMasked || IsCompressed)
                        ? PayloadData.ApplicationData.ToText()
                        : PayloadData.ToString();

            return $@"
                    FIN: {Fin}
                   RSV1: {Rsv1}
                   RSV2: {Rsv2}
                   RSV3: {Rsv3}
                 Opcode: {Opcode}
                   MASK: {Mask}
         Payload Length: {payloadLen}
Extended Payload Length: {extPayloadLen}
            Masking Key: {maskingKey}
           Payload Data: {payload}";
        }

        public byte[] ToArray()
        {
            using (var buff = new MemoryStream())
            {
                var header = (int)Fin;

                header = (header << 1) + (int)Rsv1;
                header = (header << 1) + (int)Rsv2;
                header = (header << 1) + (int)Rsv3;
                header = (header << 4) + (int)Opcode;
                header = (header << 1) + (int)Mask;
                header = (header << 7) + (int)PayloadLength;
                buff.Write(((ushort)header).ToByteArray(Endianness.Big), 0, 2);

                if (PayloadLength > 125)
                    buff.Write(ExtendedPayloadLength, 0, PayloadLength == 126 ? 2 : 8);

                if (Mask == Mask.On)
                    buff.Write(MaskingKey, 0, 4);

                if (PayloadLength > 0)
                {
                    var bytes = PayloadData.ToArray();
                    if (PayloadLength < 127)
                    {
                        buff.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        using (var input = new MemoryStream(bytes))
                            input.CopyTo(buff, 1024);
                    }
                }

                return buff.ToArray();
            }
        }

        public override string ToString() => BitConverter.ToString(ToArray());

        internal static WebSocketFrame CreateCloseFrame(PayloadData payloadData, bool mask) => new WebSocketFrame(Fin.Final, Opcode.Close, payloadData, false, mask);

        internal static WebSocketFrame CreatePingFrame(bool mask) => new WebSocketFrame(Fin.Final, Opcode.Ping, new PayloadData(), false, mask);

        internal static WebSocketFrame CreatePingFrame(byte[] data, bool mask) => new WebSocketFrame(Fin.Final, Opcode.Ping, new PayloadData(data), false, mask);
        
        internal void Validate(WebSocket webSocket)
        {
            var masked = IsMasked;

            if (webSocket.IsClient && masked)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "A frame from the server is masked.");
            }

            if (!webSocket.IsClient && !masked)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError, "A frame from a client isn't masked.");
            }

            if (webSocket.InContinuation && (Opcode == Opcode.Text || Opcode == Opcode.Binary))
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "A data frame has been received while receiving continuation frames.");
            }

            if (IsCompressed && webSocket.Compression == CompressionMethod.None)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "A compressed frame has been received without any agreement for it.");
            }

            if (Rsv2 == Rsv.On)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "The RSV2 of a frame is non-zero without any negotiation for it.");
            }

            if (Rsv3 == Rsv.On)
            {
                throw new WebSocketException(CloseStatusCode.ProtocolError,
                    "The RSV3 of a frame is non-zero without any negotiation for it.");
            }
        }

        internal void Unmask()
        {
            if (Mask == Mask.Off)
                return;

            Mask = Mask.Off;
            PayloadData.Mask(MaskingKey);
            MaskingKey = WebSocket.EmptyBytes;
        }

        private static byte[] CreateMaskingKey()
        {
            var key = new byte[4];
            WebSocketKey.RandomNumber.GetBytes(key);

            return key;
        }

        private static bool IsOpcodeData(Opcode opcode) => opcode == Opcode.Text || opcode == Opcode.Binary;
    }
}
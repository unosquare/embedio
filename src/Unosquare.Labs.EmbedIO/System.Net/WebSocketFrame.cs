#if !NET46
#region License
/*
 * WebSocketFrame.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2015 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Chris Swiedler
 */
#endregion
 
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

namespace Unosquare.Net
{
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
        Final = 0x1
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
        On = 0x1
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
        On = 0x1
    }
    internal class WebSocketFrame : IEnumerable<byte>
    {
#region Internal Fields

        /// <summary>
        /// Represents the ping frame without the payload data as an array of <see cref="byte"/>.
        /// </summary>
        /// <remarks>
        /// The value of this field is created from a non masked frame, so it can only be used to
        /// send a ping from a server.
        /// </remarks>
        internal static readonly byte[] EmptyPingBytes;

#endregion

#region Static Constructor

        static WebSocketFrame()
        {
            EmptyPingBytes = CreatePingFrame(false).ToArray();
        }

#endregion

#region Private Constructors

        private WebSocketFrame()
        {
        }

#endregion

#region Internal Constructors

        internal WebSocketFrame(Opcode opcode, PayloadData payloadData, bool mask)
          : this(Fin.Final, opcode, payloadData, false, mask)
        {
        }

        internal WebSocketFrame(Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask)
          : this(fin, opcode, new PayloadData(data), compressed, mask)
        {
        }

        internal WebSocketFrame(
          Fin fin, Opcode opcode, PayloadData payloadData, bool compressed, bool mask)
        {
            Fin = fin;
            Rsv1 = opcode.IsData() && compressed ? Rsv.On : Rsv.Off;
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
                ExtendedPayloadLength = ((ushort)len).InternalToByteArray(Swan.Endianness.Big);
            }
            else
            {
                PayloadLength = (byte)127;
                ExtendedPayloadLength = len.InternalToByteArray(Swan.Endianness.Big);
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

#endregion

#region Internal Properties

        internal int ExtendedPayloadLengthCount => PayloadLength < 126 ? 0 : (PayloadLength == 126 ? 2 : 8);

        internal ulong FullPayloadLength => PayloadLength < 126
            ? PayloadLength
            : PayloadLength == 126
                ? ExtendedPayloadLength.ToUInt16(Swan.Endianness.Big)
                : ExtendedPayloadLength.ToUInt64(Swan.Endianness.Big);

#endregion

#region Public Properties

        public byte[] ExtendedPayloadLength { get; private set; }

        public Fin Fin { get; private set; }

        public bool IsBinary => Opcode == Opcode.Binary;

        public bool IsClose => Opcode == Opcode.Close;

        public bool IsCompressed => Rsv1 == Rsv.On;

        public bool IsContinuation => Opcode == Opcode.Cont;

        public bool IsControl => Opcode >= Opcode.Close;

        public bool IsData => Opcode == Opcode.Text || Opcode == Opcode.Binary;

        public bool IsFinal => Fin == Fin.Final;

        public bool IsFragment => Fin == Fin.More || Opcode == Opcode.Cont;

        public bool IsMasked => Mask == Mask.On;

        public bool IsPing => Opcode == Opcode.Ping;

        public bool IsPong => Opcode == Opcode.Pong;

        public bool IsText => Opcode == Opcode.Text;

        public ulong Length => 2 + (ulong)(ExtendedPayloadLength.Length + MaskingKey.Length) + PayloadData.Length;

        public Mask Mask { get; private set; }

        public byte[] MaskingKey { get; private set; }

        public Opcode Opcode { get; private set; }

        public PayloadData PayloadData { get; private set; }

        public byte PayloadLength { get; private set; }

        public Rsv Rsv1 { get; private set; }

        public Rsv Rsv2 { get; private set; }

        public Rsv Rsv3 { get; private set; }

#endregion

#region Private Methods

        private static byte[] CreateMaskingKey()
        {
            var key = new byte[4];
            WebSocket.RandomNumber.GetBytes(key);

            return key;
        }
        
        private static WebSocketFrame ProcessHeader(byte[] header)
        {
            if (header.Length != 2)
                throw new WebSocketException("The header of a frame cannot be read from the stream.");

            // FIN
            var fin = (header[0] & 0x80) == 0x80 ? Fin.Final : Fin.More;

            // RSV1
            var rsv1 = (header[0] & 0x40) == 0x40 ? Rsv.On : Rsv.Off;

            // RSV2
            var rsv2 = (header[0] & 0x20) == 0x20 ? Rsv.On : Rsv.Off;

            // RSV3
            var rsv3 = (header[0] & 0x10) == 0x10 ? Rsv.On : Rsv.Off;

            // Opcode
            var opcode = (byte)(header[0] & 0x0f);

            // MASK
            var mask = (header[1] & 0x80) == 0x80 ? Mask.On : Mask.Off;

            // Payload Length
            var payloadLen = (byte)(header[1] & 0x7f);

            var err = !opcode.IsSupported()
                      ? "An unsupported opcode."
                      : !opcode.IsData() && rsv1 == Rsv.On
                        ? "A non data frame is compressed."
                        : opcode.IsControl() && fin == Fin.More
                          ? "A control frame is fragmented."
                          : opcode.IsControl() && payloadLen > 125
                            ? "A control frame has a long payload length."
                            : null;

            if (err != null)
                throw new WebSocketException(CloseStatusCode.ProtocolError, err);

            return new WebSocketFrame
            {
                Fin = fin,
                Rsv1 = rsv1,
                Rsv2 = rsv2,
                Rsv3 = rsv3,
                Opcode = (Opcode) opcode,
                Mask = mask,
                PayloadLength = payloadLen
            };
        }

        private static void ReadExtendedPayloadLength(Stream stream, WebSocketFrame frame)
        {
            var len = frame.ExtendedPayloadLengthCount;
            if (len == 0)
            {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;
                return;
            }

            var bytes = stream.ReadBytes(len);
            if (bytes.Length != len)
                throw new WebSocketException(
                  "The extended payload length of a frame cannot be read from the stream.");

            frame.ExtendedPayloadLength = bytes;
        }

        private static void ReadExtendedPayloadLengthAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error)
        {
            var len = frame.ExtendedPayloadLengthCount;
            if (len == 0)
            {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;
                completed(frame);

                return;
            }

            stream.ReadBytesAsync(
              len,
              bytes =>
              {
                  if (bytes.Length != len)
                      throw new WebSocketException(
                  "The extended payload length of a frame cannot be read from the stream.");

                  frame.ExtendedPayloadLength = bytes;
                  completed(frame);
              },
              error);
        }

        private static WebSocketFrame ReadHeader(Stream stream)
        {
            return ProcessHeader(stream.ReadBytes(2));
        }

        private static void ReadHeaderAsync(
          Stream stream, Action<WebSocketFrame> completed, Action<Exception> error)
        {
            stream.ReadBytesAsync(2, bytes => completed(ProcessHeader(bytes)), error);
        }

        private static void ReadMaskingKey(Stream stream, WebSocketFrame frame)
        {
            var len = frame.IsMasked ? 4 : 0;
            if (len == 0)
            {
                frame.MaskingKey = WebSocket.EmptyBytes;
                return;
            }

            var bytes = stream.ReadBytes(len);
            if (bytes.Length != len)
                throw new WebSocketException("The masking key of a frame cannot be read from the stream.");

            frame.MaskingKey = bytes;
        }

        private static void ReadMaskingKeyAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error)
        {
            var len = frame.IsMasked ? 4 : 0;
            if (len == 0)
            {
                frame.MaskingKey = WebSocket.EmptyBytes;
                completed(frame);

                return;
            }

            stream.ReadBytesAsync(
              len,
              bytes =>
              {
                  if (bytes.Length != len)
                      throw new WebSocketException(
                  "The masking key of a frame cannot be read from the stream.");

                  frame.MaskingKey = bytes;
                  completed(frame);
              },
              error);
        }

        private static WebSocketFrame ReadPayloadData(Stream stream, WebSocketFrame frame)
        {
            var len = frame.FullPayloadLength;
            if (len == 0)
            {
                frame.PayloadData = PayloadData.Empty;
                return frame;
            }

            if (len > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");

            var llen = (long)len;
            var bytes = frame.PayloadLength < 127
                        ? stream.ReadBytes((int)len)
                        : stream.ReadBytes(llen, 1024);

            if (bytes.Length != llen)
                throw new WebSocketException(
                  "The payload data of a frame cannot be read from the stream.");

            frame.PayloadData = new PayloadData(bytes, llen);
            return frame;
        }

        private static void ReadPayloadDataAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error)
        {
            var len = frame.FullPayloadLength;
            if (len == 0)
            {
                frame.PayloadData = PayloadData.Empty;
                completed(frame);

                return;
            }

            if (len > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");

            var llen = (long)len;
            Action<byte[]> compl = bytes =>
            {
                if (bytes.Length != llen)
                    throw new WebSocketException(
                      "The payload data of a frame cannot be read from the stream.");

                frame.PayloadData = new PayloadData(bytes, llen);
                completed(frame);
            };

            if (frame.PayloadLength < 127)
            {
                stream.ReadBytesAsync((int)len, compl, error);
                return;
            }

            stream.ReadBytesAsync(llen, 1024, compl, error);
        }

#endregion

#region Internal Methods

        internal static WebSocketFrame CreateCloseFrame(PayloadData payloadData, bool mask)
        {
            return new WebSocketFrame(Fin.Final, Opcode.Close, payloadData, false, mask);
        }

        internal static WebSocketFrame CreatePingFrame(bool mask)
        {
            return new WebSocketFrame(Fin.Final, Opcode.Ping, PayloadData.Empty, false, mask);
        }

        internal static WebSocketFrame CreatePingFrame(byte[] data, bool mask)
        {
            return new WebSocketFrame(Fin.Final, Opcode.Ping, new PayloadData(data), false, mask);
        }

        internal static WebSocketFrame ReadFrame(Stream stream, bool unmask)
        {
            var frame = ReadHeader(stream);
            ReadExtendedPayloadLength(stream, frame);
            ReadMaskingKey(stream, frame);
            ReadPayloadData(stream, frame);

            if (unmask)
                frame.Unmask();

            return frame;
        }

        internal static void ReadFrameAsync(
          Stream stream, bool unmask, Action<WebSocketFrame> completed, Action<Exception> error)
        {
            ReadHeaderAsync(
              stream,
              frame =>
                ReadExtendedPayloadLengthAsync(
                  stream,
                  frame,
                  frame1 =>
                    ReadMaskingKeyAsync(
                      stream,
                      frame1,
                      frame2 =>
                        ReadPayloadDataAsync(
                          stream,
                          frame2,
                          frame3 =>
                          {
                              if (unmask)
                                  frame3.Unmask();

                              completed(frame3);
                          },
                          error),
                      error),
                  error),
              error);
        }

        internal void Unmask()
        {
            if (Mask == Mask.Off)
                return;

            Mask = Mask.Off;
            PayloadData.Mask(MaskingKey);
            MaskingKey = WebSocket.EmptyBytes;
        }

#endregion

#region Public Methods

        public IEnumerator<byte> GetEnumerator()
        {
            return ((IEnumerable<byte>) ToArray()).GetEnumerator();
        }
        
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
                            : IsText && !(IsFragment || IsMasked || IsCompressed)
                              ? Encoding.UTF8.GetString(PayloadData.ApplicationData)
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
                buff.Write(((ushort)header).InternalToByteArray(Swan.Endianness.Big), 0, 2);

                if (PayloadLength > 125)
                    buff.Write(ExtendedPayloadLength, 0, PayloadLength == 126 ? 2 : 8);

                if (Mask == Mask.On)
                    buff.Write(MaskingKey, 0, 4);

                if (PayloadLength > 0)
                {
                    var bytes = PayloadData.ToArray();
                    if (PayloadLength < 127)
                        buff.Write(bytes, 0, bytes.Length);
                    else
                        buff.WriteBytes(bytes, 1024);
                }

#if NET452
                buff.Close();
#endif
                return buff.ToArray();
            }
        }

        public override string ToString()
        {
            return BitConverter.ToString(ToArray());
        }

#endregion

#region Explicit Interface Implementations

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#endregion
    }
}
#endif
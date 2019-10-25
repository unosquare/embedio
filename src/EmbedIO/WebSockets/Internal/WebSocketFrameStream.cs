using System;
using System.IO;
using System.Threading.Tasks;
using Swan;

namespace EmbedIO.WebSockets.Internal
{
    internal class WebSocketFrameStream
    {
        private readonly bool _unmask;
        private readonly Stream? _stream;

        public WebSocketFrameStream(Stream? stream, bool unmask = false)
        {
            _stream = stream;
            _unmask = unmask;
        }

        internal async Task<WebSocketFrame?> ReadFrameAsync(WebSocket webSocket)
        {
            if (_stream == null) return null;

            var frame = ProcessHeader(await _stream.ReadBytesAsync(2).ConfigureAwait(false));

            await ReadExtendedPayloadLengthAsync(frame).ConfigureAwait(false);
            await ReadMaskingKeyAsync(frame).ConfigureAwait(false);
            await ReadPayloadDataAsync(frame).ConfigureAwait(false);

            if (_unmask)
                frame.Unmask();

            frame.Validate(webSocket);

            frame.Unmask();

            return frame;
        }
        
        private static bool IsOpcodeData(byte opcode) => opcode == 0x1 || opcode == 0x2;
        
        private static bool IsOpcodeControl(byte opcode) => opcode > 0x7 && opcode < 0x10;

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

            var err = !Enum.IsDefined(typeof(Opcode), opcode)
                ? "An unsupported opcode."
                : !IsOpcodeData(opcode) && rsv1 == Rsv.On
                    ? "A non data frame is compressed."
                    : IsOpcodeControl(opcode) && fin == Fin.More
                        ? "A control frame is fragmented."
                        : IsOpcodeControl(opcode) && payloadLen > 125
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
                Opcode = (Opcode)opcode,
                Mask = mask,
                PayloadLength = payloadLen,
            };
        }
        
        private async Task ReadExtendedPayloadLengthAsync(WebSocketFrame frame)
        {
            var len = frame.ExtendedPayloadLengthCount;

            if (len == 0)
            {
                frame.ExtendedPayloadLength = Array.Empty<byte>();
                return;
            }

            var bytes = await _stream.ReadBytesAsync(len).ConfigureAwait(false);

            if (bytes.Length != len)
            {
                throw new WebSocketException(
                    "The extended payload length of a frame cannot be read from the stream.");
            }

            frame.ExtendedPayloadLength = bytes;
        }

        private async Task ReadMaskingKeyAsync(WebSocketFrame frame)
        {
            var len = frame.IsMasked ? 4 : 0;

            if (len == 0)
            {
                frame.MaskingKey = Array.Empty<byte>();
                return;
            }

            var bytes = await _stream.ReadBytesAsync(len).ConfigureAwait(false);
            if (bytes.Length != len)
            {
                throw new WebSocketException(
                      "The masking key of a frame cannot be read from the stream.");
            }

            frame.MaskingKey = bytes;
        }

        private async Task ReadPayloadDataAsync(WebSocketFrame frame)
        {
            var len = frame.FullPayloadLength;
            if (len == 0)
            {
                frame.PayloadData = new PayloadData();

                return;
            }

            if (len > PayloadData.MaxLength)
                throw new WebSocketException(CloseStatusCode.TooBig, "A frame has a long payload length.");

            var bytes = frame.PayloadLength < 127
                ? await _stream.ReadBytesAsync((int)len).ConfigureAwait(false)
                : await _stream.ReadBytesAsync((int)len, 1024).ConfigureAwait(false);

            if (bytes.Length != (int)len)
            {
                throw new WebSocketException(
                      "The payload data of a frame cannot be read from the stream.");
            }

            frame.PayloadData = new PayloadData(bytes);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Swan;

namespace EmbedIO.WebSockets.Internal
{
    internal class WebSocketStream : MemoryStream
    {
        internal const int FragmentLength = 1016;

        private readonly CompressionMethod _compression;
        private readonly Opcode _opcode;

        public WebSocketStream(byte[] data, Opcode opcode, CompressionMethod compression)
            : base(data)
        {
            _compression = compression;
            _opcode = opcode;
        }

        public IEnumerable<WebSocketFrame> GetFrames()
        {
            var compressed = _compression != CompressionMethod.None;
            var stream = compressed
                ? this.CompressAsync(_compression, true, CancellationToken.None).Await()
                : this;

            var len = stream.Length;

            /* Not fragmented */

            if (len == 0)
            {
                yield return new WebSocketFrame(Fin.Final, _opcode, Array.Empty<byte>(), compressed);
                yield break;
            }

            var quo = len / FragmentLength;
            var rem = (int)(len % FragmentLength);

            byte[] buff;

            if (quo == 0)
            {
                buff = new byte[rem];

                if (stream.Read(buff, 0, rem) == rem)
                    yield return new WebSocketFrame(Fin.Final, _opcode, buff, compressed);

                yield break;
            }

            buff = new byte[FragmentLength];
            if (quo == 1 && rem == 0)
            {
                if (stream.Read(buff, 0, FragmentLength) == FragmentLength)
                    yield return new WebSocketFrame(Fin.Final, _opcode, buff, compressed);

                yield break;
            }

            /* Send fragmented */

            // Begin
            if (stream.Read(buff, 0, FragmentLength) != FragmentLength)
                yield break;

            yield return new WebSocketFrame(Fin.More, _opcode, buff, compressed);

            var n = rem == 0 ? quo - 2 : quo - 1;
            for (var i = 0; i < n; i++)
            {
                if (stream.Read(buff, 0, FragmentLength) != FragmentLength)
                    yield break;

                yield return new WebSocketFrame(Fin.More, Opcode.Cont, buff, compressed);
            }

            // End
            if (rem == 0)
                rem = FragmentLength;
            else
                buff = new byte[rem];

            if (stream.Read(buff, 0, rem) == rem)
                yield return new WebSocketFrame(Fin.Final, Opcode.Cont, buff, compressed);
        }
    }
}
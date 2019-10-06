using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.WebSockets.Internal
{
    internal static class StreamExtensions
    {
        private static readonly byte[] LastByte = { 0x00 };

        // Compresses or decompresses a stream using the specified compression method.
        public static async Task<MemoryStream> CompressAsync(
            this Stream @this,
            CompressionMethod method,
            bool compress,
            CancellationToken cancellationToken)
        {
            @this.Position = 0;
            var targetStream = new MemoryStream();

            switch (method)
            {
                case CompressionMethod.Deflate:
                    if (compress)
                    {
                        using var compressor = new DeflateStream(targetStream, CompressionMode.Compress, true);
                        await @this.CopyToAsync(compressor, 1024, cancellationToken).ConfigureAwait(false);
                        await @this.CopyToAsync(compressor).ConfigureAwait(false);

                        // WebSocket use this
                        targetStream.Write(LastByte, 0, 1);
                        targetStream.Position = 0;
                    }
                    else
                    {
                        using var compressor = new DeflateStream(@this, CompressionMode.Decompress);
                        await compressor.CopyToAsync(targetStream).ConfigureAwait(false);
                    }

                    break;
                case CompressionMethod.Gzip:
                    if (compress)
                    {
                        using var compressor = new GZipStream(targetStream, CompressionMode.Compress, true);
                        await @this.CopyToAsync(compressor).ConfigureAwait(false);
                    }
                    else
                    {
                        using var compressor = new GZipStream(@this, CompressionMode.Decompress);
                        await compressor.CopyToAsync(targetStream).ConfigureAwait(false);
                    }

                    break;
                case CompressionMethod.None:
                    await @this.CopyToAsync(targetStream).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }

            return targetStream;
        }
    }
}
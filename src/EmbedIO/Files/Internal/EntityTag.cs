using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Files.Internal
{
    internal static class EntityTag
    {
        public const long MaxSizeForStrongTag = WebServer.StreamCopyBufferSize;

        public static string Compute(DateTime lastModifiedUtc, byte[] content)
        {
            if (content.Length > MaxSizeForStrongTag)
                return MakeWeakTag(lastModifiedUtc, content.Length);

            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            {
                md5.AppendData(BuildHeader(lastModifiedUtc));
                md5.AppendData(content);
                return MakeStrongTag(md5.GetHashAndReset());
            }
        }

        public static async Task<string> ComputeAsync(IFileProvider provider, MappedFileInfo info, CompressionMethod compressionMethod, CancellationToken cancellationToken)
        {
            if (info.Size > MaxSizeForStrongTag)
                return MakeWeakTag(info.LastWriteTimeUtc, info.Size);

            using (var source = provider.OpenFile(info.Path))
            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            {
                md5.AppendData(BuildHeader(info.LastWriteTimeUtc));
                var buffer = new byte[WebServer.StreamCopyBufferSize];
                int read;
                switch (compressionMethod)
                {
                    case CompressionMethod.Deflate:
                    {
                        using (var memoryStream = new MemoryStream())
                        using (var compressionStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
                        {
                            for (; ; )
                            {
                                read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                    .ConfigureAwait(false);

                                if (read == 0)
                                    break;

                                await compressionStream.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                                md5.AppendData(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                                memoryStream.Position = 0;
                            }
                        }

                        break;
                    }

                    case CompressionMethod.Gzip:
                    {
                        using (var memoryStream = new MemoryStream())
                        using (var compressionStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                        {
                            for (; ; )
                            {
                                read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                    .ConfigureAwait(false);

                                if (read == 0)
                                    break;

                                await compressionStream.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                                md5.AppendData(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
                                memoryStream.Position = 0;
                            }
                        }

                        break;
                    }

                    default:
                    {
                        for (; ; )
                        {
                            read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                .ConfigureAwait(false);

                            if (read == 0)
                                break;

                            md5.AppendData(buffer, 0, read);
                        }

                        break;
                    }
                }

                return MakeStrongTag(md5.GetHashAndReset());
            }
        }

        public static async Task<string> ComputeWhileCopyingStreamAsync(DateTime lastModifiedUtc, Stream source, Stream target, CancellationToken cancellationToken)
        {
            using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
            {
                md5.AppendData(BuildHeader(lastModifiedUtc));
                var buffer = new byte[WebServer.StreamCopyBufferSize];
                for (; ; )
                {
                    var read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);

                    if (read == 0)
                        break;

                    md5.AppendData(buffer, 0, read);
                    await target.WriteAsync(buffer, 0, read, cancellationToken)
                        .ConfigureAwait(false);
                }

                return MakeStrongTag(md5.GetHashAndReset());
            }
        }

        private static Stream CreateCompressionStream(Stream source, CompressionMethod compressionMethod, bool leaveOpen)
        {
            switch (compressionMethod)
            {
                case CompressionMethod.Deflate:
                    return new DeflateStream(source, CompressionMode.Compress, leaveOpen);
                case CompressionMethod.Gzip:
                    return new GZipStream(source, CompressionMode.Compress, leaveOpen);
                default:
                    return null;
            }
        }

        private static string MakeWeakTag(DateTime lastModifiedUtc, long size)
        {
            var dateBytes = BitConverter.GetBytes(lastModifiedUtc.Ticks);
            var sizeBytes = BitConverter.GetBytes(size);
            var bytes = new byte[] {
                dateBytes[0], sizeBytes[0],
                dateBytes[1], sizeBytes[1],
                dateBytes[2], sizeBytes[2],
                dateBytes[3], sizeBytes[3],
                dateBytes[4], sizeBytes[4],
                dateBytes[5], sizeBytes[5],
                dateBytes[6], sizeBytes[6],
                dateBytes[7], sizeBytes[7],
            };

            return "W/\"" + Convert.ToBase64String(bytes).TrimEnd('=') + "\"";
        }

        private static byte[] BuildHeader(DateTime lastModifiedUtc)
            => BitConverter.GetBytes(lastModifiedUtc.Ticks);

        private static string MakeStrongTag(byte[] bytes)
            => "\"" + Convert.ToBase64String(bytes, Base64FormattingOptions.None).TrimEnd('=') + "\"";
    }
}
using System.IO;
using System.IO.Compression;

namespace EmbedIO.Internal
{
    internal static class CompressionUtility
    {
        public static byte[]? ConvertCompression(byte[] source, CompressionMethod sourceMethod, CompressionMethod targetMethod)
        {
            if (source == null)
                return null;

            if (sourceMethod == targetMethod)
                return source;

            switch (sourceMethod)
            {
                case CompressionMethod.Deflate:
                    using (var sourceStream = new MemoryStream(source, false))
                    {
                        using var decompressionStream = new DeflateStream(sourceStream, CompressionMode.Decompress, true);
                        using var targetStream = new MemoryStream();
                        if (targetMethod == CompressionMethod.Gzip)
                        {
                            using var compressionStream = new GZipStream(targetStream, CompressionMode.Compress, true);
                            decompressionStream.CopyTo(compressionStream);
                        }
                        else
                        {
                            decompressionStream.CopyTo(targetStream);
                        }

                        return targetStream.ToArray();
                    }

                case CompressionMethod.Gzip:
                    using (var sourceStream = new MemoryStream(source, false))
                    {
                        using var decompressionStream = new GZipStream(sourceStream, CompressionMode.Decompress, true);
                        using var targetStream = new MemoryStream();
                        if (targetMethod == CompressionMethod.Deflate)
                        {
                            using var compressionStream = new DeflateStream(targetStream, CompressionMode.Compress, true);
                            decompressionStream.CopyToAsync(compressionStream);
                        }
                        else
                        {
                            decompressionStream.CopyTo(targetStream);
                        }

                        return targetStream.ToArray();
                    }

                default:
                    using (var sourceStream = new MemoryStream(source, false))
                    {
                        using var targetStream = new MemoryStream();
                        switch (targetMethod)
                        {
                            case CompressionMethod.Deflate:
                                using (var compressionStream = new DeflateStream(targetStream, CompressionMode.Compress, true))
                                    sourceStream.CopyTo(compressionStream);

                                break;

                            case CompressionMethod.Gzip:
                                using (var compressionStream = new GZipStream(targetStream, CompressionMode.Compress, true))
                                    sourceStream.CopyTo(compressionStream);

                                break;

                            default:
                                // Just in case. Consider all other values as None.
                                return source;
                        }

                        return targetStream.ToArray();
                    }
            }
        }
    }
}
using System;
using System.Text;

namespace EmbedIO.Files.Internal
{
    internal static class EntityTag
    {
        public static string Compute(DateTime lastModifiedUtc, long length, CompressionMethod compressionMethod)
        {
            var sb = new StringBuilder()
                .Append('"')
                .Append(Base64Utility.LongToBase64(lastModifiedUtc.Ticks))
                .Append(Base64Utility.LongToBase64(length));

            switch (compressionMethod)
            {
                case CompressionMethod.Deflate:
                    sb.Append('-').Append(CompressionMethodNames.Deflate);
                    break;
                case CompressionMethod.Gzip:
                    sb.Append('-').Append(CompressionMethodNames.Gzip);
                    break;
            }

            return sb.Append('"').ToString();
        }
    }
}
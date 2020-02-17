using System;

namespace EmbedIO.Files.Internal
{
    internal static class Base64Utility
    {
        // long is 8 bytes
        // base64 of 8 bytes is 12 chars, but the last one is padding
        public static string LongToBase64(long value)
            => Convert.ToBase64String(BitConverter.GetBytes(value)).Substring(0, 11);
    }
}
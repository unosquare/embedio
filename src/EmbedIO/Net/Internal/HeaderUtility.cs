using System;
using System.Linq;

namespace EmbedIO.Net.Internal
{
    internal static class HeaderUtility
    {
        public static string? GetCharset(string? contentType)
            => contentType?
                .Split(';')
                .Select(p => p.Trim())
                .Where(part => part.StartsWith("charset", StringComparison.OrdinalIgnoreCase))
                .Select(GetAttributeValue)
                .FirstOrDefault();

        public static string? GetAttributeValue(string nameAndValue)
        {
            var idx = nameAndValue.IndexOf('=');

            return idx < 0 || idx == nameAndValue.Length - 1 ? null : nameAndValue.Substring(idx + 1).Trim().Unquote();
        }
    }
}
using System;

namespace EmbedIO.Internal
{
    internal static class UriUtility
    {
        public static Uri StringToUri(string str)
        {
            _ = Uri.TryCreate(str, CanBeAbsoluteUrl(str) ? UriKind.Absolute : UriKind.Relative, out var result);
            return result;
        }

        public static Uri? StringToAbsoluteUri(string str)
        {
            if (!CanBeAbsoluteUrl(str))
            {
                return null;
            }

            _ = Uri.TryCreate(str, UriKind.Absolute, out var result);
            return result;
        }

        // Returns true if string starts with "http:", "https:", "ws:", or "wss:"
        private static bool CanBeAbsoluteUrl(string str)
            => !string.IsNullOrEmpty(str)
            && str[0] switch {
                   'h' => str.Length >= 5
                       && str[1] == 't'
                       && str[2] == 't'
                       && str[3] == 'p'
                       && str[4] switch {
                              ':' => true,
                              's' => str.Length >= 6 && str[5] == ':',
                              _ => false
                          },
                   'w' => str.Length >= 3
                       && str[1] == 's'
                       && str[2] switch {
                              ':' => true,
                              's' => str.Length >= 4 && str[3] == ':',
                              _ => false
                          },
                   _ => false
               };
    }
}
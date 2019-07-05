using System;
using System.Collections.Specialized;

namespace EmbedIO.Internal
{
    internal static class UrlEncodedDataParser
    {
        // Parses form data from a request body.
        internal static NameValueCollection Parse(string source)
        {
            var result = new LockableNameValueCollection();

            // Verify there is data to parse; otherwise, return an empty collection.
            if (source == null)
            {
                result.MakeReadOnly();
                return result;
            }

            var length = source.Length;
            if (length == 0)
            {
                result.MakeReadOnly();
                return result;
            }

            // If source is the Query property of a Uri, it can start with a question mark,
            // that we better skip.
            var kvpPos = source[0] == '?' ? 1 : 0;
            while (kvpPos < length)
            {
                var separatorPos = source.IndexOf('&', kvpPos);
                if (separatorPos < 0)
                    separatorPos = length;

                var kvp = source.Substring(kvpPos, separatorPos - kvpPos);

                // We don't want empty KVPs
                if (kvp.Length == 0)
                    continue;

                // Split by the equals char into key and value.
                // Some KVPS will have only their key, some will have both key and value
                // Some other might be repeated which really means an array
                string key;
                string value;
                var equalPos = kvp.IndexOf('=');
                if (equalPos < 0)
                {
                    key = kvp;
                    value = string.Empty;
                }
                else
                {
                    key = kvp.Substring(0, equalPos);
                    value = kvp.Substring(equalPos + 1);
                }

                // Decode the key.
                key = System.Net.WebUtility.UrlDecode(key);

                // Discard bracketed index (used e.g. by PHP)
                var bracketPos = key.IndexOf("[", StringComparison.Ordinal);
                if (bracketPos > 0)
                    key = key.Substring(0, bracketPos);

                // Decode the value.
                value = System.Net.WebUtility.UrlDecode(value);

                // Add the KVP to the collection.
                result.Add(key, value);

                // On to next KVP
                kvpPos = separatorPos + 1;
            }

            // The result is read-only so it can be cached.
            result.MakeReadOnly();
            return result;
        }
    }
}
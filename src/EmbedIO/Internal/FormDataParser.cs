using System;
using System.Collections.Specialized;

namespace EmbedIO.Internal
{
    internal static class FormDataParser
    {
        // Parses form data from a request body.
        internal static NameValueCollection Parse(string requestBody)
        {
            var result = new LockableNameValueCollection();

            // Verify there is data to parse
            if (requestBody == null)
                return null;

            var length = requestBody.Length;
            var kvpPos = 0;
            while (kvpPos < length)
            {
                var separatorPos = requestBody.IndexOf('&', kvpPos);
                if (separatorPos < 0)
                    separatorPos = length;

                var kvp = requestBody.Substring(kvpPos, separatorPos - kvpPos);

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
                    value = null;
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
                if (value != null)
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
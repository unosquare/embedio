using System;
using System.Collections.Specialized;
using System.Net;
using EmbedIO.Internal;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Parses URL queries or URL-encoded HTML forms.
    /// </summary>
    public static class UrlEncodedDataParser
    {
        /// <summary>
        /// <para>Parses a URL query or URL-encoded HTML form.</para>
        /// <para>Unlike <see cref="HttpListenerRequest.QueryString" />, the returned
        /// <see cref="NameValueCollection" /> will have bracketed indexes stripped away;
        /// for example, <c>a[0]=1&amp;a[1]=2</c> will yield the same result as <c>a=1&amp;a=2</c>,
        /// i.e. a <see cref="NameValueCollection" /> with one key (<c>a</c>) associated with
        /// two values (<c>1</c> and <c>2</c>).</para>
        /// </summary>
        /// <param name="source">The string to parse.</param>
        /// <param name="groupFlags"><para>If this parameter is <see langword="true" />,
        /// tokens not followed by an equal sign (e.g. <c>this</c> in <c>a=1&amp;this&amp;b=2</c>)
        /// will be grouped as values of a <c>null</c> key.
        /// This is the same behavior as the <see cref="IHttpRequest.QueryString" /> and
        /// <see cref="HttpListenerRequest.QueryString" /> properties.</para>
        /// <para>If this parameter is <see langword="false" />, tokens not followed by an equal sign
        /// (e.g. <c>this</c> in <c>a=1&amp;this&amp;b=2</c>) will be considered keys with an empty
        /// value. This is the same behavior as the <see cref="HttpContextExtensions.GetRequestQueryData" />
        /// extension method.</para></param>
        /// <param name="mutableResult"><see langword="true" /> (the default) to return
        /// a mutable (non-read-only) collection; <see langword="false" /> to return a read-only collection.</param>
        /// <returns>A <see cref="NameValueCollection" /> containing the parsed data.</returns>
        public static NameValueCollection Parse(string source, bool groupFlags, bool mutableResult = true)
        {
            var result = new LockableNameValueCollection();

            // Verify there is data to parse; otherwise, return an empty collection.
            if (string.IsNullOrEmpty(source))
            {
                if (!mutableResult)
                    result.MakeReadOnly();

                return result;
            }

            void AddKeyValuePair(string? key, string value)
            {
                if (key != null)
                {
                    // Decode the key.
                    key = WebUtility.UrlDecode(key);

                    // Discard bracketed index (used e.g. by PHP)
                    var bracketPos = key.IndexOf("[", StringComparison.Ordinal);
                    if (bracketPos > 0)
                        key = key.Substring(0, bracketPos);
                }

                // Decode the value.
                value = WebUtility.UrlDecode(value);

                // Add the KVP to the collection.
                result.Add(key, value);
            }

            // Skip the initial question mark,
            // in case source is the Query property of a Uri.
            var kvpPos = source[0] == '?' ? 1 : 0;
            var length = source.Length;
            while (kvpPos < length)
            {
                var separatorPos = kvpPos;
                var equalPos = -1;

                while (separatorPos < length)
                {
                    var c = source[separatorPos];
                    if (c == '&')
                        break;

                    if (c == '=' && equalPos < 0)
                        equalPos = separatorPos;

                    separatorPos++;
                }

                // Split by the equals char into key and value.
                // Some KVPS will have only their key, some will have both key and value
                // Some other might be repeated which really means an array
                if (equalPos < 0)
                {
                    if (groupFlags)
                    {
                        AddKeyValuePair(null, source.Substring(kvpPos, separatorPos - kvpPos));
                    }
                    else
                    {
                        AddKeyValuePair(source.Substring(kvpPos, separatorPos - kvpPos), string.Empty);
                    }
                }
                else
                {
                    AddKeyValuePair(
                        source.Substring(kvpPos, equalPos - kvpPos),
                        source.Substring(equalPos + 1, separatorPos - equalPos - 1));
                }

                // Edge case: if the last character in source is '&',
                // there's an empty KVP that we would otherwise skip.
                if (separatorPos == length - 1)
                {
                    AddKeyValuePair(groupFlags ? null : string.Empty, string.Empty);
                    break;
                }

                // On to next KVP
                kvpPos = separatorPos + 1;
            }

            if (!mutableResult)
                result.MakeReadOnly();

            return result;
        }
    }
}
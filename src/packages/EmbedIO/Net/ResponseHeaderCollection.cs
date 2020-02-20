using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using EmbedIO.Utilities;

namespace EmbedIO.Net
{
    /// <summary>
    /// <para>Contains protocol headers associated with an HTTP response.</para>
    /// <para>Unlike <see cref="WebHeaderCollection"/>, that supports a variety of scenarios,
    /// this class is only meant to be used for the <see cref="IHttpResponse.Headers">Headers</see>
    /// property of the <see cref="IHttpResponse"/> interface. Therefore, some header names
    /// are restricted and cannot be read or set through an instance of this class.</para>
    /// <para>The restricted headers are the following:</para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Header name</term>
    ///     <description>Rationale</description>
    ///   </listheader>
    ///   <item>
    ///     <term>
    ///       <para><c>Connection</c></para>
    ///       <para><c>Content-Encoding</c></para>
    ///       <para><c>Content-Length</c></para>
    ///       <para><c>Date</c></para>
    ///       <para><c>Keep-Alive</c></para>
    ///       <para><c>Sec-Web-Socket-Accept</c></para>
    ///       <para><c>Sec-Web-Socket-Extensions</c></para>
    ///       <para><c>Sec-Web-Socket-Protocol</c></para>
    ///       <para><c>Sec-Web-Socket-Version</c></para>
    ///       <para><c>Server</c></para>
    ///       <para><c>Trailer</c></para>
    ///       <para><c>Transfer-Encoding</c></para>
    ///     </term>
    ///     <description>These headers are automatically managed by EmbedIO.</description>
    ///   </item>
    ///   <item>
    ///     <term>
    ///       <para><c>Set-Cookie</c></para>
    ///       <para><c>Set-Cookie2</c></para>
    ///     </term>
    ///     <description>Cookies are set through the <see cref="IHttpMessage.Cookies"/> property.</description>
    ///   </item>
    ///   <item>
    ///     <term>
    ///       <para><c>Accept</c></para>
    ///       <para><c>Accept-Charset</c></para>
    ///       <para><c>Accept-Encoding</c></para>
    ///       <para><c>Accept-Language</c></para>
    ///       <para><c>Accept-Patch</c></para>
    ///       <para><c>Accept-Ranges</c></para>
    ///       <para><c>Access-Control-Request-Headers</c></para>
    ///       <para><c>Access-Control-Request-Method</c></para>
    ///       <para><c>Authorization</c></para>
    ///       <para><c>Cookie</c></para>
    ///       <para><c>Cookie2</c></para>
    ///       <para><c>Expect</c></para>
    ///       <para><c>From</c></para>
    ///       <para><c>Host</c></para>
    ///       <para><c>If-Match</c></para>
    ///       <para><c>If-Modified-Since</c></para>
    ///       <para><c>If-None-Match</c></para>
    ///       <para><c>If-Range</c></para>
    ///       <para><c>If-Unmodified-Since</c></para>
    ///       <para><c>Origin</c></para>
    ///       <para><c>Proxy-Authorization</c></para>
    ///       <para><c>Range</c></para>
    ///       <para><c>Referer</c></para>
    ///       <para><c>Sec-Web-Socket-Key</c></para>
    ///       <para><c>TE</c></para>
    ///       <para><c>Upgrade</c></para>
    ///       <para><c>Upgrade-Insecure-Requests</c></para>
    ///       <para><c>User-Agent</c></para>
    ///     </term>
    ///     <description>These headers are only meant to be included in HTTP requests, not in responses.</description>
    ///   </item>
    ///   <item>
    ///     <term>
    ///       <para><c>Age</c></para>
    ///       <para><c>Proxy-Authentication</c></para>
    ///       <para><c>Proxy-Connection</c></para>
    ///       <para><c>Via</c></para>
    ///     </term>
    ///     <description>These headers are only meant to be sent by proxies, not servers.</description>
    ///   </item>
    /// </list>
    /// </summary>
    /// <seealso cref="NameValueCollection" />
#pragma warning disable CA1010 // Implement generic ICollection - NameValueCollection does not, so neither does this class
    public sealed class ResponseHeaderCollection : NameValueCollection
#pragma warning restore CA1010
    {
        private static readonly string[] RestrictedNames = CreateRestrictedNames();
        private static readonly string[] MultiValueNames = CreateMultiValueNames();

        private readonly NameValueCollection _innerCollection = new NameValueCollection(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public override int Count => _innerCollection.Count;

        /// <inheritdoc />
        public override KeysCollection Keys => _innerCollection.Keys;

        /// <inheritdoc />
        public override string[] AllKeys => _innerCollection.AllKeys!;

        /// <summary>
        /// <para>Determines whether a specified header name is restricted, i.e. its value cannot be read or set
        /// through a <see cref="ResponseHeaderCollection"/>.</para>
        /// <para>See the documentation for <see cref="ResponseHeaderCollection"/>
        /// for a list of restricted header names.</para>
        /// </summary>
        /// <param name="name">The header name to test.</param>
        /// <returns><see langword="true"/> if <paramref name="name"/> is a restricted header name;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsRestrictedName(string name) => Array.BinarySearch(RestrictedNames, name, StringComparer.OrdinalIgnoreCase) >= 0;

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="name"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="name"/> is not a valid HTTP header name.
        /// See the documentation for <see cref="Validate.HttpHeaderName"/> for more information.</para>
        /// <para>- or -</para>
        /// <para><paramref name="name"/> is a restricted header name. See the documentation for
        /// <see cref="ResponseHeaderCollection"/> for a list of restricted header names.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains one or more characters not allowed in an HTTP header value.
        /// See the documentation for <see cref="Validate.HttpHeaderValue"/> for more information.</para>
        /// </exception>
        public override void Set(string name, string value)
        {
            name = Validate.HttpHeaderName(nameof(name), name);
            EnsureNotRestrictedName(nameof(name), name);
            value = Validate.HttpHeaderValue(nameof(value), value);

            InvalidateCachedArrays();
            _innerCollection.Set(name, value);
        }

        /// <inheritdoc />
        public override string[]? GetValues(int index) => _innerCollection.GetValues(index);

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="name"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="name"/> is not a valid HTTP header name.
        /// See the documentation for <see cref="Validate.HttpHeaderName"/> for more information.</para>
        /// <para>- or -</para>
        /// <para><paramref name="name"/> is a restricted header name. See the documentation for
        /// <see cref="ResponseHeaderCollection"/> for a list of restricted header names.</para>
        /// </exception>
        public override string[]? GetValues(string name)
        {
            name = Validate.HttpHeaderName(nameof(name), name);
            EnsureNotRestrictedName(nameof(name), name);

            var values = _innerCollection.GetValues(name);
            if (values == null || values.Length == 0 || !AllowsMultipleValues(name))
                return values;

            return values.Where(v => !string.IsNullOrEmpty(v)).Aggregate(new List<string>(), (result, value) =>
            {
                result.AddRange(ParseMultipleValues(value));
                return result;
            }).ToArray();
        }

        /// <inheritdoc />
        public override string GetKey(int index) => _innerCollection.GetKey(index);

        /// <inheritdoc />
        public override void Clear()
        {
            InvalidateCachedArrays();
            _innerCollection.Clear();
        }

        /// <inheritdoc />
        public override string Get(int index) => _innerCollection.Get(index);

        /// <inheritdoc />
        public override string Get(string name) => _innerCollection.Get(name);

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="name"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="name"/> is not a valid HTTP header name.
        /// See the documentation for <see cref="Validate.HttpHeaderName"/> for more information.</para>
        /// <para>- or -</para>
        /// <para><paramref name="name"/> is a restricted header name. See the documentation for
        /// <see cref="ResponseHeaderCollection"/> for a list of restricted header names.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains one or more characters not allowed in an HTTP header value.
        /// See the documentation for <see cref="Validate.HttpHeaderValue"/> for more information.</para>
        /// </exception>
        public override void Add(string name, string value)
        {
            name = Validate.HttpHeaderName(nameof(name), name);
            EnsureNotRestrictedName(nameof(name), name);
            value = Validate.HttpHeaderValue(nameof(value), value);

            InvalidateCachedArrays();
            _innerCollection.Add(name, value);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="name"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="name"/> is not a valid HTTP header name.
        /// See the documentation for <see cref="Validate.HttpHeaderName"/> for more information.</para>
        /// <para>- or -</para>
        /// <para><paramref name="name"/> is a restricted header name. See the documentation for
        /// <see cref="ResponseHeaderCollection"/> for a list of restricted header names.</para>
        /// </exception>
        public override void Remove(string name)
        {
            name = Validate.HttpHeaderName(nameof(name), name);
            EnsureNotRestrictedName(nameof(name), name);

            InvalidateCachedArrays();
            _innerCollection.Remove(name);
        }

        /// <inheritdoc />
        public override int GetHashCode() => _innerCollection.GetHashCode();

        /// <inheritdoc />
        public override string ToString()
        {
            if (Count == 0)
                return "\r\n";

            var sb = new StringBuilder(30 * Count);
            foreach (string key in _innerCollection.Keys)
            {
                sb
                .Append(key)
                .Append(": ")
                .Append(_innerCollection.Get(key))
                .Append("\r\n");
            }

            sb.Append("\r\n");
            return sb.ToString();
        }

        /// <inheritdoc />
        public override IEnumerator GetEnumerator() => _innerCollection.Keys.GetEnumerator();

        /// <inheritdoc />
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
            => _innerCollection.GetObjectData(info, context);

        /// <inheritdoc />
        public override void OnDeserialization(object sender)
            => _innerCollection.OnDeserialization(sender);

        internal void CopyTo(NameValueCollection other)
        {
            foreach (string key in _innerCollection.Keys)
                other.Set(key, _innerCollection.Get(key));
        }

        private static bool AllowsMultipleValues(string name) => Array.BinarySearch(MultiValueNames, name, StringComparer.OrdinalIgnoreCase) >= 0;

        private static IReadOnlyList<string> ParseMultipleValues(string value)
        {
            var result = new List<string>();
            var inQuotes = false;
            var startIndex = 0;
            var length = 0;

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (value[i] == ',' && !inQuotes)
                {
                    result.Add(value.TrimmedSubstring(startIndex, length));
                    startIndex = i + 1;
                    length = 0;
                    continue;
                }

                length++;
            }

            if (startIndex < value.Length && length > 0)
                result.Add(value.TrimmedSubstring(startIndex, length));

            return result;
        }

        // NOTE TO COLLABORATORS: Always keep the class XML docs in sync with this list!
        private static string[] CreateRestrictedNames()
        {
            var result = new List<string>
            {
                // Set automatically by EmbedIO
                HttpHeaderNames.Connection,
                HttpHeaderNames.ContentEncoding,
                HttpHeaderNames.ContentLength,
                HttpHeaderNames.Date,
                HttpHeaderNames.KeepAlive,
                HttpHeaderNames.SecWebSocketAccept,
                HttpHeaderNames.SecWebSocketExtensions,
                HttpHeaderNames.SecWebSocketProtocol,
                HttpHeaderNames.SecWebSocketVersion,
                HttpHeaderNames.Server,
                HttpHeaderNames.Trailer,
                HttpHeaderNames.TransferEncoding,

                // Cookies can only be managed through a Cookies collection
                HttpHeaderNames.SetCookie,
                HttpHeaderNames.SetCookie2,

                // Request-only headers
                HttpHeaderNames.Accept,
                HttpHeaderNames.AcceptCharset,
                HttpHeaderNames.AcceptEncoding,
                HttpHeaderNames.AcceptLanguage,
                HttpHeaderNames.AcceptPatch,
                HttpHeaderNames.AcceptRanges,
                HttpHeaderNames.AccessControlRequestHeaders,
                HttpHeaderNames.AccessControlRequestMethod,
                HttpHeaderNames.Authorization,
                HttpHeaderNames.Cookie,
                HttpHeaderNames.Cookie2,
                HttpHeaderNames.Expect,
                HttpHeaderNames.From,
                HttpHeaderNames.Host,
                HttpHeaderNames.IfMatch,
                HttpHeaderNames.IfModifiedSince,
                HttpHeaderNames.IfNoneMatch,
                HttpHeaderNames.IfRange,
                HttpHeaderNames.IfUnmodifiedSince,
                HttpHeaderNames.Origin,
                HttpHeaderNames.ProxyAuthorization,
                HttpHeaderNames.Range,
                HttpHeaderNames.Referer,
                HttpHeaderNames.SecWebSocketKey,
                HttpHeaderNames.TE,
                HttpHeaderNames.Upgrade,
                HttpHeaderNames.UpgradeInsecureRequests,
                HttpHeaderNames.UserAgent,

                // Forbidden header names
                HttpHeaderNames.Age,
                HttpHeaderNames.ProxyConnection,
                HttpHeaderNames.Via,
            };

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.ToArray();
        }

        private static string[] CreateMultiValueNames()
        {
            var result = new List<string>
            {
                HttpHeaderNames.Allow,
                HttpHeaderNames.CacheControl,
                HttpHeaderNames.ContentLanguage,
                HttpHeaderNames.Vary,
            };

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result.ToArray();
        }

        private void EnsureNotRestrictedName(string argumentName, string headerName)
        {
            if (IsRestrictedName(headerName))
                throw new ArgumentException($"{headerName} cannot be got or set through a Headers collection.", argumentName);
        }
    }
}
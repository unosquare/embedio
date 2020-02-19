using System;
using System.Linq;
using System.Net;

namespace EmbedIO.Utilities
{
    partial class Validate
    {
        private static readonly char[] HttpTrimCharacters = { '\t', ' ' };

        /// <summary>
        /// <para>Ensures that a <see langword="string"/> argument is valid as an HTTP header name as defined by
        /// <see href="https://tools.ietf.org/html/rfc7230#section-3.2">RFC7230, Section 3.2</see>.</para>
        /// <para>The rules for valid HTTP header names are defined by RFC7230 to be the same as for
        /// RFC2616 tokens; for further information, please see the documentation for the
        /// <see cref="Rfc2616Token"/> method.</para>
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/>, if it is a valid HTTP header name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains one or more characters that are not allowed in a HTTP header name.</para>
        /// </exception>
        public static string HttpHeaderName(string argumentName, [ValidatedNotNull] string value)
            => Rfc2616Token(argumentName, value, "Header name");

        /// <summary>
        /// <para>Ensures that a <see langword="string"/> argument is valid as an HTTP header value as defined by
        /// <see href="https://tools.ietf.org/html/rfc7230#section-3.2">RFC7230, Section 3.2</see>.</para>
        /// <para>Only visible ASCII visible characters, plus space and horizontal tab, are allowed.</para>
        /// <para><see langword="null"/> is allowed, but it is turned into the empty string.</para>
        /// <para>Obsolete line folding (see <see href="https://tools.ietf.org/html/rfc7230#section-3.2.4">Section 3.2.4</see>)
        /// is NOT allowed, even if it is still allowed by Microsoft's <see cref="WebHeaderCollection"/>.</para>
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/>, if it is a valid HTTP header value, with spaces
        /// and horizontal tabs trimmed on both ends..</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> contains one or more characters
        /// that are not allowed in a HTTP header value.</exception>
        public static string HttpHeaderValue(string argumentName, string value)
        {
            // Allow null but turn it into an empty string
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // RFC7230, Section 3.2
            // Only USASCII visible characters +  space + horizontal tab are allowed in field content.
            // As per Section 3.2.4 no line folding is allowed (even if Microsoft's WebHeaderCollection still allows it)
            if (!value.All(c => c == '\t' || (c >= ' ' && c <= '~')))
                throw new ArgumentException("Header value contains one or more invalid characters.", argumentName);

            return value.Trim(HttpTrimCharacters);
        }
    }
}
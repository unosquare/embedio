using System;
using System.Linq;

namespace EmbedIO.Utilities
{
    partial class Validate
    {
        private static readonly char[] ValidRfc2616TokenChars = GetValidRfc2616TokenChars();

        /// <summary>
        /// <para>Ensures that a <see langword="string"/> argument is valid as a token as defined by
        /// <see href="https://tools.ietf.org/html/rfc2616#section-2.2">RFC2616, Section 2.2</see>.</para>
        /// <para>RFC2616 tokens are used, for example, as:</para>
        /// <list type="bullet">
        /// <item><description>cookie names, as stated in <see href="https://tools.ietf.org/html/rfc6265#section-4.1.1">RFC6265, Section 4.1.1</see>;</description></item>
        /// <item><description>WebSocket protocol names, as stated in <see href="https://tools.ietf.org/html/rfc6455#section-4.3">RFC6455, Section 4.3</see>.</description></item>
        /// </list>
        /// <para>Only a restricted set of characters are allowed in tokens, including:</para>
        /// <list type="bullet">
        /// <item><description>upper- and lower-case letters of the English alphabet;</description></item>
        /// <item><description>decimal digits;</description></item>
        /// <item><description>the following non-alphanumeric characters:
        /// <c>!</c>, <c>#</c>, <c>$</c>, <c>%</c>, <c>&amp;</c>, <c>'</c>, <c>*</c>, <c>+</c>,
        /// <c>-</c>, <c>.</c>, <c>^</c>, <c>_</c>, <c>`</c>, <c>|</c>, <c>~</c>.</description></item>
        /// </list>
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <returns><paramref name="value"/>, if it is a valid token.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains one or more characters that are not allowed in a token.</para>
        /// </exception>
        public static string Rfc2616Token(string argumentName, string value)
        {
            value = NotNullOrEmpty(argumentName, value);

            if (!IsRfc2616Token(value))
                throw new ArgumentException("Token contains one or more invalid characters.", argumentName);

            return value;
        }

        internal static bool IsRfc2616Token(string value)
            => !string.IsNullOrEmpty(value)
            && !value.Any(c => c < '\x21' || c > '\x7E' || Array.BinarySearch(ValidRfc2616TokenChars, c) < 0);

        private static char[] GetValidRfc2616TokenChars()
            => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&'*+-.^_`|~"
                .OrderBy(c => c)
                .ToArray();
    }
}
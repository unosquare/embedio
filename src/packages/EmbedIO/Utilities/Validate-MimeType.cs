using System;

namespace EmbedIO.Utilities
{
    partial class Validate
    {
        /// <summary>
        /// <para>Ensures that a <see langword="string"/> argument is valid as MIME type or media range as defined by
        /// <see href="https://tools.ietf.org/html/rfc7231#section-5.3.2">RFC7231, Section 5,3.2</see>.</para>
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="acceptMediaRange">If <see langword="true"/>, media ranges (i.e. strings of the form <c>*/*</c>
        /// and <c>type/*</c>) are considered valid; otherwise, they are rejected as invalid.</param>
        /// <returns><paramref name="value"/>, if it is a valid MIME type or media range.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is the empty string.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> is not a valid MIME type or media range.</para>
        /// </exception>
        public static string MimeType(string argumentName, string value, bool acceptMediaRange)
        {
            value = NotNullOrEmpty(argumentName, value);

            if (!EmbedIO.MimeType.IsMimeType(value, acceptMediaRange))
                throw new ArgumentException("MIME type is not valid.", argumentName);

            return value;
        }
    }
}
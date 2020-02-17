using System;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Provides constants for commonly-used MIME types and association between file extensions and MIME types.
    /// </summary>
    /// <seealso cref="Associations"/>
    public static partial class MimeType
    {
        /// <summary>
        /// The default MIME type for data whose type is unknown,
        /// i.e. <c>application/octet-stream</c>.
        /// </summary>
        public const string Default = "application/octet-stream";

        /// <summary>
        /// The MIME type for plain text, i.e. <c>text/plain</c>.
        /// </summary>
        public const string PlainText = "text/plain";

        /// <summary>
        /// The MIME type for HTML, i.e. <c>text/html</c>.
        /// </summary>
        public const string Html = "text/html";

        /// <summary>
        /// The MIME type for JSON, i.e. <c>application/json</c>.
        /// </summary>
        public const string Json = "application/json";

        /// <summary>
        /// The MIME type for URL-encoded HTML forms,
        /// i.e. <c>application/x-www-form-urlencoded</c>.
        /// </summary>
        internal const string UrlEncodedForm = "application/x-www-form-urlencoded";

        /// <summary>
        /// <para>Strips parameters, if present (e.g. <c>; encoding=UTF-8</c>), from a MIME type.</para>
        /// </summary>
        /// <param name="value">The MIME type.</param>
        /// <returns><paramref name="value"/> without parameters.</returns>
        /// <remarks>
        /// <para>This method does not validate <paramref name="value"/>: if it is not
        /// a valid MIME type or media range, it is just returned unchanged.</para>
        /// </remarks>
        public static string StripParameters(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var semicolonPos = value.IndexOf(';');
            return semicolonPos < 0
                ? value
                : value.Substring(0, semicolonPos).TrimEnd();
        }

        /// <summary>
        /// Determines whether the specified string is a valid MIME type or media range.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="acceptMediaRange">If set to <see langword="true"/>, both media ranges
        /// (e.g. <c>"text/*"</c>, <c>"*/*"</c>) and specific MIME types (e.g. <c>"text/html"</c>)
        /// are considered valid; if set to <see langword="false"/>, only specific MIME types
        /// are considered valid.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is valid,
        /// according to the value of <paramref name="acceptMediaRange"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsMimeType(string value, bool acceptMediaRange)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var slashPos = value.IndexOf('/');
            if (slashPos < 0)
                return false;

            var isWildcardSubtype = false;
            var subtype = value.Substring(slashPos + 1);
            if (subtype == "*")
            {
                if (!acceptMediaRange)
                    return false;

                isWildcardSubtype = true;
            }
            else if (!Validate.IsRfc2616Token(subtype))
            {
                return false;
            }

            var type = value.Substring(0, slashPos);
            return type == "*"
                ? acceptMediaRange && isWildcardSubtype
                : Validate.IsRfc2616Token(type);
        }

        /// <summary>
        /// Splits the specified MIME type or media range into type and subtype.
        /// </summary>
        /// <param name="mimeType">The MIME type or media range to split.</param>
        /// <returns>A tuple of type and subtype.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/> is not a valid
        /// MIME type or media range.</exception>
        public static (string type, string subtype) Split(string mimeType)
            => UnsafeSplit(Validate.MimeType(nameof(mimeType), mimeType, true));

        /// <summary>
        /// Matches the specified MIME type to a media range.
        /// </summary>
        /// <param name="mimeType">The MIME type to match.</param>
        /// <param name="mediaRange">The media range.</param>
        /// <returns><see langword="true"/> if <paramref name="mediaRange"/> is either
        /// the same as <paramref name="mimeType"/>, or has the same type and a subtype
        /// of <c>"*"</c>, or is <c>"*/*"</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="mimeType"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="mediaRange"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="mimeType"/> is not a valid MIME type.</para>
        /// <para>- or -</para>
        /// <para><paramref name="mediaRange"/> is not a valid MIME media range.</para>
        /// </exception>
        public static bool IsInRange(string mimeType, string mediaRange)
            => UnsafeIsInRange(
                Validate.MimeType(nameof(mimeType), mimeType, false),
                Validate.MimeType(nameof(mediaRange), mediaRange, true));

        internal static (string type, string subtype) UnsafeSplit(string mimeType)
        {
            var slashPos = mimeType.IndexOf('/');
            return (mimeType.Substring(0, slashPos), mimeType.Substring(slashPos + 1));
        }

        internal static bool UnsafeIsInRange(string mimeType, string mediaRange)
        {
            // A validated media range that starts with '*' can only be '*/*'
            if (mediaRange[0] == '*')
                return true;

            var typeSlashPos = mimeType.IndexOf('/');
            var rangeSlashPos = mediaRange.IndexOf('/');

            if (typeSlashPos != rangeSlashPos)
                return false;

            for (var i = 0; i < typeSlashPos; i++)
            {
                if (mimeType[i] != mediaRange[i])
                    return false;
            }

            // A validated token has at least 1 character,
            // thus there must be at least 1 character after a slash.
            if (mediaRange[rangeSlashPos + 1] == '*')
                return true;

            if (mimeType.Length != mediaRange.Length)
                return false;

            for (var i = typeSlashPos + 1; i < mimeType.Length; i++)
            {
                if (mimeType[i] != mediaRange[i])
                    return false;
            }

            return true;
        }
    }
}
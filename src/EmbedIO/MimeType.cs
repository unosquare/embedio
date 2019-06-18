namespace EmbedIO
{
    /// <summary>
    /// Provides constants for commonly-used MIME types and association between file extensions and MIME types.
    /// </summary>
    /// <seealso cref="Associations"/>
    public static partial class MimeType
    {
        /// <summary>
        /// The default MIME type for data whose type is unknown.
        /// </summary>
        public const string Default = "application/octet-stream";

        /// <summary>
        /// The MIME type for plain text.
        /// </summary>
        public const string PlainText = "text/plain";

        /// <summary>
        /// The MIME type for HTML.
        /// </summary>
        public const string Html = "text/html";

        /// <summary>
        /// The MIME type for JSON.
        /// </summary>
        public const string Json = "application/json";

        /// <summary>
        /// The MIME type for URL-encoded HTML forms.
        /// </summary>
        internal const string UrlEncodedForm = "application/x-www-form-urlencoded";
    }
}
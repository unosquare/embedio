namespace EmbedIO
{
    /// <summary>
    /// Represents an object that can manage custom MIME type associations.
    /// </summary>
    /// <seealso cref="IMimeTypeProvider" />
    public interface IMimeTypeCustomizer : IMimeTypeProvider
    {
        /// <summary>
        /// Adds a custom association between a file extension and a MIME type.
        /// </summary>
        /// <param name="extension">The file extension to associate to <paramref name="mimeType"/>.</param>
        /// <param name="mimeType">The MIME type to associate to <paramref name="extension"/>.</param>
        void AddCustomMimeType(string extension, string mimeType);
    }
}
namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IMimeTypeCustomizer"/>.
    /// </summary>
    public static class MimeTypeCustomizerExtensions
    {
        /// <summary>
        /// Adds a custom association between a file extension and a MIME type.
        /// </summary>
        /// <typeparam name="T">The type of the object to which this method is applied.</typeparam>
        /// <param name="this">The object to which this method is applied.</param>
        /// <param name="extension">The file extension to associate to <paramref name="mimeType"/>.</param>
        /// <param name="mimeType">The MIME type to associate to <paramref name="extension"/>.</param>
        /// <returns><paramref name="this"/> with the custom association added.</returns>
        public static T WithCustomMimeType<T>(this T @this, string extension, string mimeType)
            where T : IMimeTypeCustomizer
        {
            @this.AddCustomMimeType(extension, mimeType);
            return @this;
        }
    }
}
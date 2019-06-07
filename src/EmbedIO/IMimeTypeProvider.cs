namespace EmbedIO
{
    /// <summary>
    /// Represents an object that can associate a file extension to a MIME type.
    /// </summary>
    public interface IMimeTypeProvider
    {
        /// <summary>
        /// Attempts to get the MIME type asociated to a file extension.
        /// </summary>
        /// <param name="extension">The file extension for which a corresponding MIME type is wanted.</param>
        /// <param name="mimeType">If this method returns <see langword="true"/>, the MIME type
        /// associated with <paramref name="extension"/>; otherwise, <see langword="null"/>.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a corresponding MIME type has been found;
        /// otherwise, <see langword="false"/>.</returns>
        bool TryGetMimeType(string extension, out string mimeType);
    }
}
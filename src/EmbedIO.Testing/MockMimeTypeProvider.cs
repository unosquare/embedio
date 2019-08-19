namespace EmbedIO.Testing
{
    /// <summary>
    /// Provides an <see cref="IMimeTypeProvider"/> interface
    /// that associates all extensions to <c>application/octet-stream</c>
    /// and never suggests any data compression preference.
    /// </summary>
    /// <seealso cref="IMimeTypeProvider" />
    public class MockMimeTypeProvider : IMimeTypeProvider
    {
        /// <inheritdoc />
        /// <remarks>
        /// <see cref="MockMimeTypeProvider"/> always returns <see cref="MimeType.Default"/>
        /// (<c>application/octet-stream</c>).
        /// </remarks>
        public string GetMimeType(string extension) => MimeType.Default;

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="MockMimeTypeProvider"/> always sets <paramref name="preferCompression"/>
        /// to <see langword="false"/> and returns <see langword="false"/>,
        /// </remarks>
        public bool TryDetermineCompression(string mimeType, out bool preferCompression)
        {
            preferCompression = default;
            return false;
        }
    }
}
using System;

namespace EmbedIO
{
    /// <summary>
    /// Represents an object that contains information on specific MIME types and media ranges.
    /// </summary>
    public interface IMimeTypeProvider
    {
        /// <summary>
        /// Gets the MIME type associated to a file extension.
        /// </summary>
        /// <param name="extension">The file extension for which a corresponding MIME type is wanted.</param>
        /// <returns>The MIME type corresponding to <paramref name="extension"/>, if one is found;
        /// otherwise, <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="extension"/>is <see langword="null"/>.</exception>
        string GetMimeType(string extension);

        /// <summary>
        /// Attempts to determine whether compression should be preferred
        /// when negotiating content encoding for a response with the specified content type.
        /// </summary>
        /// <param name="mimeType">The MIME type to check.</param>
        /// <param name="preferCompression">When this method returns <see langword="true"/>,
        /// a value indicating whether compression should be preferred.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if a value is found for <paramref name="mimeType"/>;
        /// otherwise, <see langword="false"/>.</returns>
        bool TryDetermineCompression(string mimeType, out bool preferCompression);
    }
}
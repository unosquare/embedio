using System;

namespace EmbedIO
{
    /// <summary>
    /// Represents an object that can associate a file extension to a MIME type.
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
    }
}
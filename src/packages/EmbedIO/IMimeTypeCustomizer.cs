using System;

namespace EmbedIO
{
    /// <summary>
    /// Represents an object that can set information about specific MIME types and media ranges,
    /// to be later retrieved via an <see cref="IMimeTypeProvider"/> interface.
    /// </summary>
    /// <seealso cref="IMimeTypeProvider" />
    public interface IMimeTypeCustomizer : IMimeTypeProvider
    {
        /// <summary>
        /// Adds a custom association between a file extension and a MIME type.
        /// </summary>
        /// <param name="extension">The file extension to associate to <paramref name="mimeType"/>.</param>
        /// <param name="mimeType">The MIME type to associate to <paramref name="extension"/>.</param>
        /// <exception cref="InvalidOperationException">The object implementing <see cref="IMimeTypeCustomizer"/>
        /// has its configuration locked.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="extension"/>is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="mimeType"/>is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="extension"/>is the empty string.</para>
        /// <para>- or -</para>
        /// <para><paramref name="mimeType"/>is not a valid MIME type.</para>
        /// </exception>
        void AddCustomMimeType(string extension, string mimeType);

        /// <summary>
        /// Indicates whether to prefer compression when negotiating content encoding
        /// for a response with the specified content type, or whose content type is in
        /// the specified media range.
        /// </summary>
        /// <param name="mimeType">The MIME type or media range.</param>
        /// <param name="preferCompression"><see langword="true"/> to prefer compression;
        /// otherwise, <see langword="false"/>.</param>
        /// <exception cref="InvalidOperationException">The object implementing <see cref="IMimeTypeCustomizer"/>
        /// has its configuration locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/>is not a valid MIME type or media range.</exception>
        void PreferCompression(string mimeType, bool preferCompression);
    }
}
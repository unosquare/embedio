using System;

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
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="this"/> has its configuration locked.</exception>
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
        public static T WithCustomMimeType<T>(this T @this, string extension, string mimeType)
            where T : IMimeTypeCustomizer
        {
            @this.AddCustomMimeType(extension, mimeType);
            return @this;
        }

        /// <summary>
        /// Indicates whether to prefer compression when negotiating content encoding
        /// for a response with the specified content type, or whose content type is in
        /// the specified media range.
        /// </summary>
        /// <typeparam name="T">The type of the object to which this method is applied.</typeparam>
        /// <param name="this">The object to which this method is applied.</param>
        /// <param name="mimeType">The MIME type or media range.</param>
        /// <param name="preferCompression"><see langword="true"/> to prefer compression;
        /// otherwise, <see langword="false"/>.</param>
        /// <returns><paramref name="this"/> with the specified preference added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="this"/> has its configuration locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/>is not a valid MIME type or media range.</exception>
        public static T PreferCompressionFor<T>(this T @this, string mimeType, bool preferCompression)
            where T : IMimeTypeCustomizer
        {
            @this.PreferCompression(mimeType, preferCompression);
            return @this;
        }

        /// <summary>
        /// Indicates that compression should be preferred when negotiating content encoding
        /// for a response with the specified content type, or whose content type is in
        /// the specified media range.
        /// </summary>
        /// <typeparam name="T">The type of the object to which this method is applied.</typeparam>
        /// <param name="this">The object to which this method is applied.</param>
        /// <param name="mimeType">The MIME type or media range.</param>
        /// <returns><paramref name="this"/> with the specified preference added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="this"/> has its configuration locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/>is not a valid MIME type or media range.</exception>
        public static T PreferCompressionFor<T>(this T @this, string mimeType)
            where T : IMimeTypeCustomizer
        {
            @this.PreferCompression(mimeType, true);
            return @this;
        }

        /// <summary>
        /// Indicates that no compression should be preferred when negotiating content encoding
        /// for a response with the specified content type, or whose content type is in
        /// the specified media range.
        /// </summary>
        /// <typeparam name="T">The type of the object to which this method is applied.</typeparam>
        /// <param name="this">The object to which this method is applied.</param>
        /// <param name="mimeType">The MIME type or media range.</param>
        /// <returns><paramref name="this"/> with the specified preference added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="this"/> has its configuration locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="mimeType"/>is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mimeType"/>is not a valid MIME type or media range.</exception>
        public static T PreferNoCompressionFor<T>(this T @this, string mimeType)
            where T : IMimeTypeCustomizer
        {
            @this.PreferCompression(mimeType, false);
            return @this;
        }
    }
}
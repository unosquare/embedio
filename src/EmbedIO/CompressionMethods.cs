namespace EmbedIO
{
    /// <summary>
    /// Exposes constants for possible values of the <c>Content-Encoding</c> HTTP header.
    /// </summary>
    /// <see cref="CompressionMethod"/>
    public static class CompressionMethods
    {
        /// <summary>
        /// Specifies no compression.
        /// </summary>
        /// <see cref="CompressionMethod.None"/>
        public const string None = "identity";

        /// <summary>
        /// Specifies the "Deflate" compression method.
        /// </summary>
        /// <see cref="CompressionMethod.Deflate"/>
        public const string Deflate = "deflate";

        /// <summary>
        /// Specifies the GZip compression method.
        /// </summary>
        /// <see cref="CompressionMethod.Gzip"/>
        public const string Gzip = "gzip";
    }
}
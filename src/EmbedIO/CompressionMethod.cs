namespace EmbedIO
{
    /// <summary>
    /// Specifies the compression method used to compress a message on
    /// the SystemWebSocket connection.
    /// </summary>
    /// <remarks>
    /// The compression methods that can be used are defined in
    /// <see href="https://tools.ietf.org/html/rfc7692">
    /// Compression Extensions for SystemWebSocket</see>.
    /// </remarks>
    public enum CompressionMethod : byte
    {
        /// <summary>
        /// Specifies no compression.
        /// </summary>
        None,

        /// <summary>
        /// Specifies "Deflate" compression.
        /// </summary>
        Deflate,

        /// <summary>
        /// Specifies GZip compression.
        /// </summary>
        Gzip,
    }
}

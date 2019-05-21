namespace EmbedIO.Constants
{
    partial class HttpHeaderNames
    {
#pragma warning disable CA1034 // Do not nest publicly accessible types
        /// <summary>
        /// Exposes constants for possible values of the <c>Compression</c> HTTP header.
        /// </summary>
        /// <see cref="CompressionMethod"/>
        public static class CompressionMethods
        {
            /// <summary>
            /// Specifies no compression.
            /// </summary>
            /// <see cref="CompressionMethod.None"/>
            public const string None = "none";

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
#pragma warning restore CA1034
    }
}
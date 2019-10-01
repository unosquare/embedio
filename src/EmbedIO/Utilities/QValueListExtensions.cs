namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="QValueList"/>.
    /// </summary>
    public static class QValueListExtensions
    {
        /// <summary>
        /// <para>Attempts to proactively negotiate a compression method for a response,
        /// based on the contents of a <see cref="QValueList"/>.</para>
        /// </summary>
        /// <param name="this">The <see cref="QValueList"/> on which this method is called.</param>
        /// <param name="preferCompression"><see langword="true"/> if sending compressed data is preferred over
        /// sending non-compressed data; otherwise, <see langword="false"/>.</param>
        /// <param name="compressionMethod">When this method returns, the compression method to use for the response,
        /// if content negotiation is successful. This parameter is passed uninitialized.</param>
        /// <param name="compressionMethodName">When this method returns, the name of the compression method,
        /// if content negotiation is successful. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if content negotiation is successful;
        /// otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>If <paramref name="this"/> is empty, this method always returns <see langword="true"/>,
        /// setting <paramref name="compressionMethod"/> to <see cref="CompressionMethod.None"/>
        /// and <paramref name="compressionMethodName"/> to <see cref="CompressionMethodNames.None"/>.</para>
        /// </remarks>
        public static bool TryNegotiateContentEncoding(
            this QValueList @this,
            bool preferCompression,
            out CompressionMethod compressionMethod,
            out string? compressionMethodName)
        {
            if (@this.QValues.Count < 1)
            {
                compressionMethod = CompressionMethod.None;
                compressionMethodName = CompressionMethodNames.None;
                return true;
            }

            // https://tools.ietf.org/html/rfc7231#section-5.3.4
            // RFC7231, Section 5.3.4, rule #2:
            // If the representation has no content-coding, then it is
            // acceptable by default unless specifically excluded by the
            // Accept - Encoding field stating either "identity;q=0" or "*;q=0"
            // without a more specific entry for "identity".
            if (!preferCompression && (!@this.TryGetWeight(CompressionMethodNames.None, out var weight) || weight > 0))
            {
                compressionMethod = CompressionMethod.None;
                compressionMethodName = CompressionMethodNames.None;
                return true;
            }

            var acceptableMethods = preferCompression
                ? new[] { CompressionMethod.Gzip, CompressionMethod.Deflate, CompressionMethod.None }
                : new[] { CompressionMethod.None, CompressionMethod.Gzip, CompressionMethod.Deflate };
            var acceptableMethodNames = preferCompression
                ? new[] { CompressionMethodNames.Gzip, CompressionMethodNames.Deflate, CompressionMethodNames.None }
                : new[] { CompressionMethodNames.None, CompressionMethodNames.Gzip, CompressionMethodNames.Deflate };

            var acceptableMethodIndex = @this.FindPreferredIndex(acceptableMethodNames);
            if (acceptableMethodIndex < 0)
            {
                compressionMethod = default;
                compressionMethodName = default;
                return false;
            }

            compressionMethod = acceptableMethods[acceptableMethodIndex];
            compressionMethodName = acceptableMethodNames[acceptableMethodIndex];
            return true;
        }
    }
}
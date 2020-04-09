using System.IO;
using System.IO.Compression;
using System.Text;
using EmbedIO.Internal;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// <para>Wraps the response output stream and returns a <see cref="Stream"/> that can be used directly.</para>
        /// <para>Optional buffering is applied, so that the response may be sent as one instead of using chunked transfer.</para>
        /// <para>Proactive negotiation is performed to select the best compression method supported by the client.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="buffered">If set to <see langword="true"/>, sent data is collected
        /// in a <see cref="MemoryStream"/> and sent all at once when the returned <see cref="Stream"/>
        /// is disposed; if set to <see langword="false"/> (the default), chunked transfer will be used.</param>
        /// <param name="preferCompression"><see langword="true"/> if sending compressed data is preferred over
        /// sending non-compressed data; otherwise, <see langword="false"/>.</param>
        /// <returns>
        /// <para>A <see cref="Stream"/> that can be used to write response data.</para>
        /// <para>This stream MUST be disposed when finished writing.</para>
        /// </returns>
        /// <seealso cref="OpenResponseText"/>
        public static Stream OpenResponseStream(this IHttpContext @this, bool buffered = false, bool preferCompression = true)
        {
            @this.Request.TryNegotiateContentEncoding(preferCompression, out var compressionMethod, out var prepareResponse);
            prepareResponse(@this.Response); // The callback will throw HttpNotAcceptableException if negotiationSuccess is false.
            var stream = buffered ? new BufferingResponseStream(@this.Response) : @this.Response.OutputStream;

            return compressionMethod switch {
                CompressionMethod.Gzip => new GZipStream(stream, CompressionMode.Compress),
                CompressionMethod.Deflate => new DeflateStream(stream, CompressionMode.Compress),
                _ => stream
            };
        }

        /// <summary>
        /// <para>Wraps the response output stream and returns a <see cref="TextWriter" /> that can be used directly.</para>
        /// <para>Optional buffering is applied, so that the response may be sent as one instead of using chunked transfer.</para>
        /// <para>Proactive negotiation is performed to select the best compression method supported by the client.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext" /> on which this method is called.</param>
        /// <param name="encoding">
        /// <para>The <see cref="Encoding"/> to use to convert text to data bytes.</para>
        /// <para>If <see langword="null"/> (the default), <see cref="WebServer.DefaultEncoding"/> (UTF-8) is used.</para>
        /// </param>
        /// <param name="buffered">If set to <see langword="true" />, sent data is collected
        /// in a <see cref="MemoryStream" /> and sent all at once when the returned <see cref="Stream" />
        /// is disposed; if set to <see langword="false" /> (the default), chunked transfer will be used.</param>
        /// <param name="preferCompression"><see langword="true"/> if sending compressed data is preferred over
        /// sending non-compressed data; otherwise, <see langword="false"/>.</param>
        /// <returns>
        /// <para>A <see cref="TextWriter" /> that can be used to write response data.</para>
        /// <para>This writer MUST be disposed when finished writing.</para>
        /// </returns>
        /// <seealso cref="OpenResponseStream"/>
        public static TextWriter OpenResponseText(this IHttpContext @this, Encoding? encoding = null, bool buffered = false, bool preferCompression = true)
        {
            encoding ??= WebServer.DefaultEncoding;
            @this.Response.ContentEncoding = encoding;
            return new StreamWriter(OpenResponseStream(@this, buffered, preferCompression), encoding);
        }
    }
}
using System.IO;
using System.IO.Compression;
using System.Text;
using Swan.Logging;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// <para>Wraps the request input stream and returns a <see cref="Stream"/> that can be used directly.</para>
        /// <para>Decompression of compressed request bodies is implemented if specified in the web server's options.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <returns>
        /// <para>A <see cref="Stream"/> that can be used to write response data.</para>
        /// <para>This stream MUST be disposed when finished writing.</para>
        /// </returns>
        /// <seealso cref="OpenRequestText"/>
        /// <seealso cref="WebServerOptionsBase.SupportCompressedRequests"/>
        public static Stream OpenRequestStream(this IHttpContext @this)
        {
            var stream = @this.Request.InputStream;

            var encoding = @this.Request.Headers[HttpHeaderNames.ContentEncoding]?.Trim();
            switch (encoding)
            {
                case CompressionMethodNames.Gzip:
                    if (@this.SupportCompressedRequests)
                        return new GZipStream(stream, CompressionMode.Decompress);
                    break;
                case CompressionMethodNames.Deflate:
                    if (@this.SupportCompressedRequests)
                        return new DeflateStream(stream, CompressionMode.Decompress);
                    break;
                case CompressionMethodNames.None:
                case null:
                    return stream;
            }

            $"[{@this.Id}] Unsupported request content encoding \"{encoding}\", sending 400 Bad Request..."
                .Warn(nameof(OpenRequestStream));

            throw HttpException.BadRequest($"Unsupported content encoding \"{encoding}\"");
        }

        /// <summary>
        /// <para>Wraps the request input stream and returns a <see cref="TextReader" /> that can be used directly.</para>
        /// <para>Decompression of compressed request bodies is implemented if specified in the web server's options.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext" /> on which this method is called.</param>
        /// <returns>
        /// <para>A <see cref="TextReader" /> that can be used to read the request body as text.</para>
        /// <para>This reader MUST be disposed when finished reading.</para>
        /// </returns>
        /// <seealso cref="OpenRequestStream"/>
        /// <seealso cref="WebServerOptionsBase.SupportCompressedRequests"/>
        public static TextReader OpenRequestText(this IHttpContext @this)
            => new StreamReader(OpenRequestStream(@this), @this.Request.ContentEncoding);
    }
}
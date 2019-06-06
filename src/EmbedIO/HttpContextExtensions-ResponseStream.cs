using System.IO;
using System.IO.Compression;
using System.Text;
using EmbedIO.Internal;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// <para>Wraps the response output stream and returns a <see cref="Stream"/> that can be used directly.</para>
        /// <para>Optional buffering is applied, so that the response may be sent as one instead of using chunked transfer.</para>
        /// <para>If no compression method (not even <see cref="CompressionMethod.None"/>is specified, uses proactive
        /// negotiation to select the best compression method supported by the client.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="buffered">If set to <see langword="true"/>, sent data is collected
        /// in a <see cref="MemoryStream"/> and sent all at once when the returned <see cref="Stream"/>
        /// is disposed; if set to <see langword="false"/> (the default), chunked transfer will be used.</param>
        /// <param name="compressionMethod">The compression method to use, or <see langword="null"/>
        /// (the default) to use proactive negotiation.</param>
        /// <returns>
        /// <para>A <see cref="Stream"/> that can be used to write response data.</para>
        /// <para>This stream MUST be disposed when finished writing.</para>
        /// </returns>
        /// <seealso cref="OpenResponseText"/>
        public static Stream OpenResponseStream(this IHttpContext @this, bool buffered = false, CompressionMethod? compressionMethod = null)
        {
            CompressionMethod compression;
            if (compressionMethod.HasValue)
            {
                compression = compressionMethod.Value;
            }
            else
            {
                var acceptedEncodings = new QValueList(true, @this.Request.Headers.GetValues(HttpHeaderNames.AcceptEncoding));
                if (acceptedEncodings.QValues.Count > 0)
                {
                    @this.Response.Headers.Add(HttpHeaderNames.Vary, HttpHeaderNames.AcceptEncoding);
                    switch (acceptedEncodings.FindPreferredIndex(
                        CompressionMethods.Gzip,
                        CompressionMethods.Deflate,
                        CompressionMethods.None))
                    {
                        case 0:
                            compression = CompressionMethod.Gzip;
                            break;
                        case 1:
                            compression = CompressionMethod.Deflate;
                            break;
                        case 2:
                            compression = CompressionMethod.None;
                            break;
                        default:
                            throw HttpException.NotAcceptable();
                    }
                }
                else
                {
                    @this.Response.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethods.Gzip);
                    compression = CompressionMethod.None;
                }
            }

            var stream = buffered ? new BufferingResponseStream(@this.Response) : @this.Response.OutputStream;
            switch (compression)
            {
                case CompressionMethod.Gzip:
                    @this.Response.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethods.Gzip);
                    return new GZipStream(stream, CompressionMode.Compress);
                case CompressionMethod.Deflate:
                    @this.Response.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethods.Deflate);
                    return new DeflateStream(stream, CompressionMode.Compress);
                default:
                    @this.Response.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethods.None);
                    return stream;
            }
        }

        /// <summary>
        /// <para>Wraps the response output stream and returns a <see cref="TextWriter" /> that can be used directly.</para>
        /// <para>Optional buffering is applied, so that the response may be sent as one instead of using chunked transfer.</para>
        /// <para>If no compression method (not even <see cref="CompressionMethod.None" />is specified, uses proactive
        /// negotiation to select the best compression method supported by the client.</para>
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext" /> on which this method is called.</param>
        /// <param name="encoding">
        /// <para>The <see cref="Encoding"/> to use to convert text to data bytes.</para>
        /// <para>If <see langword="null"/> (the default), <see cref="Encoding.UTF8">UTF-8</see> is used.</para>
        /// </param>
        /// <param name="buffered">If set to <see langword="true" />, sent data is collected
        /// in a <see cref="MemoryStream" /> and sent all at once when the returned <see cref="Stream" />
        /// is disposed; if set to <see langword="false" /> (the default), chunked transfer will be used.</param>
        /// <param name="compressionMethod">The compression method to use, or <see langword="null" />
        /// (the default) to use proactive negotiation.</param>
        /// <returns>
        /// <para>A <see cref="TextWriter" /> that can be used to write response data.</para>
        /// <para>This writer MUST be disposed when finished writing.</para>
        /// </returns>
        /// <seealso cref="OpenResponseStream"/>
        public static TextWriter OpenResponseText(this IHttpContext @this, Encoding encoding = null, bool buffered = false, CompressionMethod? compressionMethod = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            @this.Response.ContentEncoding = encoding;
            return new StreamWriter(OpenResponseStream(@this, buffered, compressionMethod), encoding);
        }
    }
}
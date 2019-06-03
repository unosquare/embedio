using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpResponseExtensions
    {
        /// <summary>
        /// Asynchronously sends the contents of a stream, starting from the stream's current position, as response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="stream">The stream whose contents must be sent.</param>
        /// <param name="useGzip">If set to <see langword="true"/>, <paramref name="stream"/>'s contents
        /// will be GZip-encoded and the response's <c>Content</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> SendStreamAsync(
            this IHttpResponse @this,
            Stream stream,
            bool useGzip,
            CancellationToken cancellationToken)
        {
            if (useGzip)
            {
                stream = await stream.CompressAsync(CompressionMethod.Gzip, cancellationToken).ConfigureAwait(false);
                @this.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethods.Gzip);
            }

            @this.ContentLength64 = stream.Length;
            stream.Position = 0;
            await @this.CopyStreamAsync(stream, cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Asynchronously sends the contents of a stream, starting from the specified position, as response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="stream">The stream whose contents must be sent.</param>
        /// <param name="position">The starting position in <paramref name="stream"/>.</param>
        /// <param name="useGzip">If set to <see langword="true"/>, <paramref name="stream"/>'s contents
        /// will be GZip-encoded and the response's <c>Content</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> SendStreamAsync(
            this IHttpResponse @this,
            Stream stream,
            long position,
            bool useGzip,
            CancellationToken cancellationToken)
        {
            if (useGzip)
            {
                stream = await stream.CompressAsync(CompressionMethod.Gzip, cancellationToken).ConfigureAwait(false);
                @this.Headers.Set(HttpHeaderNames.ContentEncoding, CompressionMethods.Gzip);
            }

            @this.ContentLength64 = stream.Length;
            stream.Position = 0;
            await @this.CopyStreamAsync(stream, position, cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Asynchronously sends a string as response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="content">The response content.</param>
        /// <param name="contentType">The MIME type of the content. If <see langword="null"/>, the content type will not be set.</param>
        /// <param name="encoding">The <see cref="Encoding"/> to use.</param>
        /// <param name="useGzip">If set to <see langword="true"/>, the <paramref name="content"/> will be GZip-encoded
        /// and the response's <c>Content</c>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="WebHandler"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="content"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="encoding"/> is <see langword="null"/>.</para>
        /// </exception>
        public static async Task<bool> SendStringAsync(
            this IHttpResponse @this,
            string content,
            string contentType,
            Encoding encoding,
            bool useGzip,
            CancellationToken cancellationToken)
        {
            content = Validate.NotNull(nameof(content), content);
            encoding = Validate.NotNull(nameof(encoding), encoding);

            if (contentType != null)
            {
                @this.ContentType = contentType;
            }

            using (var buffer = new MemoryStream(encoding.GetBytes(content)))
                return await SendStreamAsync(@this, buffer, useGzip, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="WebHandler"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="SendStandardHtmlAsync(IHttpResponse,int,Action{StringBuilder},CancellationToken)"/>
        public static Task<bool> SendStandardHtmlAsync(this IHttpResponse @this, int statusCode, CancellationToken cancellationToken)
            => SendStandardHtmlAsync(@this, statusCode, null, cancellationToken);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="appendAdditionalHtml">A callback function that may append additional HTML code
        /// to the response. If not <see langword="null"/>, the callback is called immediately before
        /// closing the HTML <c>body</c> tag.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will always be <see langword="true"/>.
        /// This allows a call to this method to be the last instruction in a <see cref="WebHandler"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="SendStandardHtmlAsync(IHttpResponse,int,CancellationToken)"/>
        public static async Task<bool> SendStandardHtmlAsync(
            this IHttpResponse @this,
            int statusCode,
            Action<StringBuilder> appendAdditionalHtml,
            CancellationToken cancellationToken)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.StatusCode = statusCode;
            @this.StatusDescription = statusDescription;
            @this.ContentType = MimeTypes.HtmlType;
            var sb = new StringBuilder()
                .Append("<html><head><meta charset=\"UTF-8\"><title>")
                .Append(statusCode)
                .Append(" - ")
                .Append(statusDescription)
                .Append("</title></head><body><h1>")
                .Append(statusCode)
                .Append(" - ")
                .Append(statusDescription)
                .Append("</h1>");
            appendAdditionalHtml?.Invoke(sb);
            sb.Append("</body></html>");
            var buffer = Encoding.UTF8.GetBytes(sb.ToString());
            sb = null; // Free some memory if next GC is near
            @this.ContentLength64 = buffer.Length;
            await @this.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }
    }
}
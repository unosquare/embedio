using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IHttpResponse"/>.
    /// </summary>
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Add the necessary headers to disable caching of a response on the client side.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static void NoCache(this IHttpResponse @this)
        {
            @this.AddHeader(HttpHeaderNames.Expires, "Mon, 26 Jul 1997 05:00:00 GMT");
            @this.AddHeader(HttpHeaderNames.LastModified, DateTime.UtcNow.ToRfc1123String());
            @this.AddHeader(HttpHeaderNames.CacheControl, "no-store, no-cache, must-revalidate");
            @this.AddHeader(HttpHeaderNames.Pragma, "no-cache");
        }

        /// <summary>
        /// Prepares a standard response without a body for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        public static void StandardResponseWithoutBody(this IHttpResponse @this, int statusCode)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.StatusCode = statusCode;
            @this.StatusDescription = statusDescription;
            @this.ContentType = string.Empty;
            @this.ContentLength64 = 0;
        }

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="StandardHtmlResponseAsync(IHttpResponse,int,Func{StringBuilder,StringBuilder},CancellationToken)"/>
        public static Task StandardHtmlResponseAsync(this IHttpResponse @this, int statusCode, CancellationToken ct)
            => StandardHtmlResponseAsync(@this, statusCode, null, ct);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="appendAdditionalHtml">A callback function that may append additional HTML code
        /// to the response. If not <see langword="null"/>, the callback is called immediately before
        /// closing the HTML <c>body</c> tag.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="StandardHtmlResponseAsync(IHttpResponse,int,CancellationToken)"/>
        public static Task StandardHtmlResponseAsync(
            this IHttpResponse @this, 
            int statusCode, 
            Func<StringBuilder, StringBuilder> appendAdditionalHtml, 
            CancellationToken ct)
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
            return @this.OutputStream.WriteAsync(buffer, 0, buffer.Length, ct);
        }

        /// <summary>
        /// Writes a binary response asynchronous.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> BinaryResponseAsync(
            this IHttpResponse response,
            Stream buffer,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            if (useGzip)
            {
                buffer = await buffer.CompressAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                response.AddHeader(HttpHeaderNames.ContentEncoding, HttpHeaderNames.CompressionMethods.Gzip);
            }

            response.ContentLength64 = buffer.Length;
            await response.WriteToOutputStream(buffer, 0, cancellationToken).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Writes to output stream.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="lowerByteIndex">Index of the lower byte.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task representing the write operation to the stream.
        /// </returns>
        public static Task WriteToOutputStream(
            this IHttpResponse response,
            Stream buffer,
            long lowerByteIndex = 0,
            CancellationToken cancellationToken = default)
        {
            buffer.Position = lowerByteIndex;
            return buffer.CopyToAsync(response.OutputStream, Modules.FileModuleBase.ChunkSize, cancellationToken);
        }

        /// <summary>
        /// Outputs async a string response given a string.
        /// </summary>
        /// <param name="this">The response.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> StringResponseAsync(
            this IHttpResponse @this,
            string content,
            string contentType,
            Encoding encoding = null,
            bool useGzip = false,
            CancellationToken cancellationToken = default)
        {
            @this.ContentType = contentType;

            using (var buffer = new MemoryStream((encoding ?? Encoding.UTF8).GetBytes(content)))
                return await BinaryResponseAsync(@this, buffer, useGzip, cancellationToken).ConfigureAwait(false);
        }
    }
}
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IHttpResponse"/>.
    /// </summary>
    public static partial class HttpResponseExtensions
    {
        /// <summary>
        /// Sets the necessary headers to disable caching of a response on the client side.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static void DisableCaching(this IHttpResponse @this)
        {
            var headers = @this.Headers;
            headers.Set(HttpHeaderNames.Expires, "Mon, 26 Jul 1997 05:00:00 GMT");
            headers.Set(HttpHeaderNames.LastModified, DateTime.UtcNow.ToRfc1123String());
            headers.Set(HttpHeaderNames.CacheControl, "no-store, no-cache, must-revalidate");
            headers.Add(HttpHeaderNames.Pragma, "no-cache");
        }

        /// <summary>
        /// Prepares a standard response without a body for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        public static void SetEmptyResponse(this IHttpResponse @this, int statusCode)
        {
            if (!HttpStatusDescription.TryGet(statusCode, out var statusDescription))
                throw new ArgumentException("Status code has no standard description.", nameof(statusCode));

            @this.StatusCode = statusCode;
            @this.StatusDescription = statusDescription;
            @this.ContentType = string.Empty;
            @this.ContentLength64 = 0;
        }

        /// <summary>
        /// Asynchronously copies the specified stream's contents, starting from the stream's current position,
        /// to a response's output stream.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="stream">The stream whose contents must be copied.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        public static Task CopyStreamAsync(this IHttpResponse @this, Stream stream, CancellationToken cancellationToken)
            => Validate.NotNull(nameof(stream), stream).CopyToAsync(@this.OutputStream, WebServer.StreamCopyBufferSize, cancellationToken);

        /// <summary>
        /// Asynchronously copies the specified stream's contents, starting from the specified position,
        /// to a response's output stream.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <param name="stream">The stream whose contents must be copied.</param>
        /// <param name="position">The starting position in <paramref name="stream"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
        public static Task CopyStreamAsync(this IHttpResponse @this, Stream stream, long position, CancellationToken cancellationToken)
        {
            stream = Validate.NotNull(nameof(stream), stream);
            stream.Position = position;
            return stream.CopyToAsync(@this.OutputStream, WebServer.StreamCopyBufferSize, cancellationToken);
        }
    }
}
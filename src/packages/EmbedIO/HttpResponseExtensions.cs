using System;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IHttpResponse"/>.
    /// </summary>
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Sets the necessary headers to disable caching of a response on the client side.
        /// </summary>
        /// <param name="this">The <see cref="IHttpResponse"/> interface on which this method is called.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static void DisableCaching(this IHttpResponse @this)
        {
            var headers = @this.Headers;
            headers.Set(HttpHeaderNames.Expires, "Sat, 26 Jul 1997 05:00:00 GMT");
            headers.Set(HttpHeaderNames.LastModified, HttpDate.Format(DateTime.UtcNow));
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
            @this.ContentType = MimeType.Default;
            @this.ContentEncoding = null;
        }
    }
}
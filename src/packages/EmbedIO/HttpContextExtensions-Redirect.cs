using System;
using System.Net;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// Sets a redirection status code and adds a <c>Location</c> header to the response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="location">The URL to which the user agent should be redirected.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="location"/> is not a valid relative or absolute URL.<see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        public static void Redirect(this IHttpContext @this, string location, int statusCode = (int)HttpStatusCode.Found)
        {
            location = Validate.Url(nameof(location), location, @this.Request.Url);

            if (statusCode < 300 || statusCode > 399)
                throw new ArgumentException("Redirect status code is not valid.", nameof(statusCode));

            @this.Response.SetEmptyResponse(statusCode);
            @this.Response.Headers[HttpHeaderNames.Location] = location;
        }
    }
}
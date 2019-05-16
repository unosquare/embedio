using System;
using System.Net;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// Sets a redirection status code and adds a <c>Location</c> header to the response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> on which this method is called.</param>
        /// <param name="location">The URL to which the user agent should be redirected.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <returns><see langword="true"/> if the status code and header were set; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="location"/> is not a valid relative or absolute URL.<see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        public static bool Redirect(this IHttpContext @this, string location, HttpStatusCode statusCode = HttpStatusCode.Found)
        {
            location = Validate.Url(nameof(location), location, @this.Request.Url);

            var status = (int)statusCode;
            if (status < 300 || status > 399)
                throw new ArgumentException("Redirect status code is not valid.", nameof(statusCode));

            @this.Response.StatusCode = status;
            @this.Response.Headers[HttpHeaders.Location] = location;

            return true;
        }
    }
}
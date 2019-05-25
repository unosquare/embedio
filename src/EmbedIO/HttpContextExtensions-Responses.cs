using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class HttpContextExtensions
    {
        /// <summary>
        /// Prepares a standard response without a body for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="HttpResponseExtensions.StandardResponseWithoutBody"/>
        public static void StandardResponseWithoutBody(this IHttpContext @this, int statusCode)
            => @this.Response.StandardResponseWithoutBody(statusCode);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="HttpResponseExtensions.StandardHtmlResponseAsync(IHttpResponse,int,CancellationToken)"/>
        public static Task StandardHtmlResponseAsync(this IHttpContext @this, int statusCode, CancellationToken cancellationToken)
            => StandardHtmlResponseAsync(@this, statusCode, null, cancellationToken);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="appendAdditionalHtml">A callback function that may append additional HTML code
        /// to the response. If not <see langword="null"/>, the callback is called immediately before
        /// closing the HTML <c>body</c> tag.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        /// <seealso cref="HttpResponseExtensions.StandardHtmlResponseAsync(IHttpResponse,int,Func{StringBuilder,StringBuilder},CancellationToken)"/>
        public static Task StandardHtmlResponseAsync(
            this IHttpContext @this, 
            int statusCode,
            Func<StringBuilder, StringBuilder> appendAdditionalHtml,
            CancellationToken cancellationToken)
            => @this.Response.StandardHtmlResponseAsync(statusCode, appendAdditionalHtml, cancellationToken);

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

            @this.Response.Headers[HttpHeaderNames.Location] = location;
            @this.Response.StandardResponseWithoutBody(statusCode);
        }
    }
}
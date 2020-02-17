using System;
using System.Net;

namespace EmbedIO
{
    /// <summary>
    /// When thrown, breaks the request handling control flow
    /// and sends a redirection response to the client.
    /// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors - they have no meaning here.
    public class HttpRedirectException : HttpException
#pragma warning restore CA1032
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRedirectException"/> class.
        /// </summary>
        /// <param name="location">The redirection target.</param>
        /// <param name="statusCode">
        /// <para>The status code to set on the response, in the range from 300 to 399.</para>
        /// <para>By default, status code 302 (<c>Found</c>) is used.</para>
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="statusCode"/> is not in the 300-399 range.</exception>
        public HttpRedirectException(string location, int statusCode = (int)HttpStatusCode.Found)
            : base(statusCode)
        {
            if (statusCode < 300 || statusCode > 399)
                throw new ArgumentException("Redirect status code is not valid.", nameof(statusCode));

            Location = location;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRedirectException"/> class.
        /// </summary>
        /// <param name="location">The redirection target.</param>
        /// <param name="statusCode">One of the redirection status codes, to be set on the response.</param>
        /// <exception cref="ArgumentException"><paramref name="statusCode"/> is not a redirection status code.</exception>
        public HttpRedirectException(string location, HttpStatusCode statusCode)
            : this(location, (int)statusCode)
        {
        }

        /// <summary>
        /// Gets the URL where the client will be redirected.
        /// </summary>
        public string Location { get; }

        /// <inheritdoc />
        public override void PrepareResponse(IHttpContext context)
        {
            context.Redirect(Location, StatusCode);
        }
    }
}
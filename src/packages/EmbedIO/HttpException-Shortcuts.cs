using System;
using System.Net;

namespace EmbedIO
{
    partial class HttpException
    {
        /// <summary>
        /// Returns a new instance of <see cref="HttpException" /> that, when thrown,
        /// will break the request handling control flow and send a <c>500 Internal Server Error</c>
        /// response to the client.
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>
        /// A newly-created <see cref="HttpException" />.
        /// </returns>
        public static HttpException InternalServerError(string? message = null, object? data = null)
            => new HttpException(HttpStatusCode.InternalServerError, message, data);
        
        /// <summary>
        /// Returns a new instance of <see cref="HttpException" /> that, when thrown,
        /// will break the request handling control flow and send a <c>401 Unauthorized</c>
        /// response to the client.
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>
        /// A newly-created <see cref="HttpException" />.
        /// </returns>
        public static HttpException Unauthorized(string? message = null, object? data = null)
            => new HttpException(HttpStatusCode.Unauthorized, message, data);

        /// <summary>
        /// Returns a new instance of <see cref="HttpException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>403 Forbidden</c>
        /// response to the client.
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>A newly-created <see cref="HttpException"/>.</returns>
        public static HttpException Forbidden(string? message = null, object? data = null)
            => new HttpException(HttpStatusCode.Forbidden, message, data);

        /// <summary>
        /// Returns a new instance of <see cref="HttpException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>400 Bad Request</c>
        /// response to the client.
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>A newly-created <see cref="HttpException"/>.</returns>
        public static HttpException BadRequest(string? message = null, object? data = null)
            => new HttpException(HttpStatusCode.BadRequest, message, data);

        /// <summary>
        /// Returns a new instance of <see cref="HttpException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>404 Not Found</c>
        /// response to the client.
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>A newly-created <see cref="HttpException"/>.</returns>
        public static HttpException NotFound(string? message = null, object? data = null)
            => new HttpException(HttpStatusCode.NotFound, message, data);

        /// <summary>
        /// Returns a new instance of <see cref="HttpException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>405 Method Not Allowed</c>
        /// response to the client.
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>A newly-created <see cref="HttpException"/>.</returns>
        public static HttpException MethodNotAllowed(string? message = null, object? data = null)
            => new HttpException(HttpStatusCode.MethodNotAllowed, message, data);

        /// <summary>
        /// Returns a new instance of <see cref="HttpNotAcceptableException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>406 Not Acceptable</c>
        /// response to the client.
        /// </summary>
        /// <returns>A newly-created <see cref="HttpNotAcceptableException"/>.</returns>
        /// <seealso cref="HttpNotAcceptableException()"/>
        public static HttpNotAcceptableException NotAcceptable() => new HttpNotAcceptableException();

        /// <summary>
        /// <para>Returns a new instance of <see cref="HttpNotAcceptableException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>406 Not Acceptable</c>
        /// response to the client.</para>
        /// </summary>
        /// <param name="vary">A value, or a comma-separated list of values, to set the response's <c>Vary</c> header to.</param>
        /// <returns>A newly-created <see cref="HttpNotAcceptableException"/>.</returns>
        /// <seealso cref="HttpNotAcceptableException(string)"/>
        public static HttpNotAcceptableException NotAcceptable(string vary) => new HttpNotAcceptableException(vary);

        /// <summary>
        /// Returns a new instance of <see cref="HttpRangeNotSatisfiableException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>416 Range Not Satisfiable</c>
        /// response to the client.
        /// </summary>
        /// <returns>A newly-created <see cref="HttpRangeNotSatisfiableException"/>.</returns>
        /// <seealso cref="HttpRangeNotSatisfiableException()"/>
        public static HttpRangeNotSatisfiableException RangeNotSatisfiable() => new HttpRangeNotSatisfiableException();

        /// <summary>
        /// Returns a new instance of <see cref="HttpRangeNotSatisfiableException"/> that, when thrown,
        /// will break the request handling control flow and send a <c>416 Range Not Satisfiable</c>
        /// response to the client.
        /// </summary>
        /// <param name="contentLength">The total length of the requested resource, expressed in bytes,
        /// or <see langword="null"/> to omit the <c>Content-Range</c> header in the response.</param>
        /// <returns>A newly-created <see cref="HttpRangeNotSatisfiableException"/>.</returns>
        /// <seealso cref="HttpRangeNotSatisfiableException()"/>
        public static HttpRangeNotSatisfiableException RangeNotSatisfiable(long? contentLength)
            => new HttpRangeNotSatisfiableException(contentLength);

        /// <summary>
        /// Returns a new instance of <see cref="HttpRedirectException" /> that, when thrown,
        /// will break the request handling control flow and redirect the client
        /// to the specified location, using response status code 302.
        /// </summary>
        /// <param name="location">The redirection target.</param>
        /// <returns>
        /// A newly-created <see cref="HttpRedirectException" />.
        /// </returns>
        public static HttpRedirectException Redirect(string location)
            => new HttpRedirectException(location);

        /// <summary>
        /// Returns a new instance of <see cref="HttpRedirectException" /> that, when thrown,
        /// will break the request handling control flow and redirect the client
        /// to the specified location, using the specified response status code.
        /// </summary>
        /// <param name="location">The redirection target.</param>
        /// <param name="statusCode">The status code to set on the response, in the range from 300 to 399.</param>
        /// <returns>
        /// A newly-created <see cref="HttpRedirectException" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="statusCode"/> is not in the 300-399 range.</exception>
        public static HttpRedirectException Redirect(string location, int statusCode)
            => new HttpRedirectException(location, statusCode);

        /// <summary>
        /// Returns a new instance of <see cref="HttpRedirectException" /> that, when thrown,
        /// will break the request handling control flow and redirect the client
        /// to the specified location, using the specified response status code.
        /// </summary>
        /// <param name="location">The redirection target.</param>
        /// <param name="statusCode">One of the redirection status codes, to be set on the response.</param>
        /// <returns>
        /// A newly-created <see cref="HttpRedirectException" />.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="statusCode"/> is not a redirection status code.</exception>
        public static HttpRedirectException Redirect(string location, HttpStatusCode statusCode)
            => new HttpRedirectException(location, statusCode);
    }
}

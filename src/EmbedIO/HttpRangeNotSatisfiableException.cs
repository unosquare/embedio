using System.Net;

namespace EmbedIO
{
    /// <summary>
    /// When thrown, breaks the request handling control flow
    /// and sends a redirection response to the client.
    /// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors - they have no meaning here.
    public class HttpRangeNotSatisfiableException : HttpException
#pragma warning restore CA1032
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRangeNotSatisfiableException"/> class.
        /// without specifying a value for the response's <c>Content-Range</c> header.
        /// </summary>
        public HttpRangeNotSatisfiableException()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpRangeNotSatisfiableException"/> class.
        /// </summary>
        /// <param name="contentLength">The total length of the requested resource, expressed in bytes,
        /// or <see langword="null"/> to omit the <c>Content-Range</c> header in the response.</param>
        public HttpRangeNotSatisfiableException(long? contentLength)
            : base((int)HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            ContentLength = contentLength;
        }

        /// <summary>
        /// Gets the total content length to be specified
        /// on the response's <c>Content-Range</c> header.
        /// </summary>
        public long? ContentLength { get; }

        /// <inheritdoc />
        public override void PrepareResponse(IHttpContext context)
        {
            // RFC 7233, Section 3.1: "When this status code is generated in response
            //                        to a byte-range request, the sender
            //                        SHOULD generate a Content-Range header field specifying
            //                        the current length of the selected representation."
            if (ContentLength.HasValue)
                context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes */{ContentLength.Value}");
        }
    }
}
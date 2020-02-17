using System.Net;

namespace EmbedIO
{
    /// <summary>
    /// When thrown, breaks the request handling control flow
    /// and sends a redirection response to the client.
    /// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors - they have no meaning here.
    public class HttpNotAcceptableException : HttpException
#pragma warning restore CA1032
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpNotAcceptableException"/> class,
        /// without specifying a value for the response's <c>Vary</c> header.
        /// </summary>
        public HttpNotAcceptableException()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpNotAcceptableException"/> class.
        /// </summary>
        /// <param name="vary">
        /// <para>A value, or a comma-separated list of values, to set the response's <c>Vary</c> header to.</para>
        /// <para>Although not specified in <see href="https://tools.ietf.org/html/rfc7231#section-6.5.6">RFC7231</see>,
        /// this may help the client to understand why the request has been rejected.</para>
        /// <para>If this parameter is <see langword="null"/> or the empty string, the response's <c>Vary</c> header
        /// is not set.</para>
        /// </param>
        public HttpNotAcceptableException(string? vary)
            : base((int)HttpStatusCode.NotAcceptable)
        {
            Vary = string.IsNullOrEmpty(vary) ? null : vary;
        }

        /// <summary>
        /// Gets the value, or comma-separated list of values, to be set
        /// on the response's <c>Vary</c> header.
        /// </summary>
        /// <remarks>
        /// <para>If the empty string has been passed to the <see cref="HttpNotAcceptableException(string)"/>
        /// constructor, the value of this property is <see langword="null"/>.</para>
        /// </remarks>
        public string? Vary { get; }

        /// <inheritdoc />
        public override void PrepareResponse(IHttpContext context)
        {
            if (Vary != null)
                context.Response.Headers.Add(HttpHeaderNames.Vary, Vary);
        }
    }
}
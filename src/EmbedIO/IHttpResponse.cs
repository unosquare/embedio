using System.IO;
using System.Net;
using System.Text;

namespace EmbedIO
{
    /// <inheritdoc />
    /// <summary>
    /// Interface to create a HTTP Response.
    /// </summary>
    public interface IHttpResponse : IHttpMessage
    {
        /// <summary>
        /// Gets the response headers.
        /// </summary>
        WebHeaderCollection Headers { get; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the content length.
        /// </summary>
        long ContentLength64 { get; set; }

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        Stream OutputStream { get; }

        /// <summary>
        /// Gets or sets the content encoding.
        /// </summary>
        Encoding? ContentEncoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [keep alive].
        /// </summary>
        bool KeepAlive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the response uses chunked transfer encoding.
        /// </summary>
        bool SendChunked { get; set; }

        /// <summary>
        /// Gets or sets a text description of the HTTP status code.
        /// </summary>
        string StatusDescription { get; set; }

        /// <summary>
        /// Sets the cookie.
        /// </summary>
        /// <param name="cookie">The session cookie.</param>
        void SetCookie(Cookie cookie);

        /// <summary>
        /// Closes this instance and dispose the resources.
        /// </summary>
        void Close();
    }
}
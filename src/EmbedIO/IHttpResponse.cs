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
        /// <value>
        /// The headers.
        /// </value>
        WebHeaderCollection Headers { get; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the content length.
        /// </summary>
        /// <value>
        /// The content length.
        /// </value>
        long ContentLength64 { get; set; }

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        string ContentType { get; set; }

        /// <summary>
        /// Gets the output stream.
        /// </summary>
        /// <value>
        /// The output stream.
        /// </value>
        Stream OutputStream { get; }

        /// <summary>
        /// Gets or sets the content encoding.
        /// </summary>
        /// <value>
        /// The content encoding.
        /// </value>
        Encoding ContentEncoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [keep alive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [keep alive]; otherwise, <c>false</c>.
        /// </value>
        bool KeepAlive { get; set; }

        /// <summary>
        /// Gets or sets a text description of the HTTP status code.
        /// </summary>
        /// <value>
        /// The status description.
        /// </value>
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
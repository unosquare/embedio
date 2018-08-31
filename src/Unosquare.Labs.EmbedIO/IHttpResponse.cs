namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Text;
    using System.IO;

    /// <summary>
    /// Interface to create a HTTP Response.
    /// </summary>
    public interface IHttpResponse
    {
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        NameValueCollection Headers { get; }

        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the content length64.
        /// </summary>
        /// <value>
        /// The content length64.
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
        /// Gets the cookies.
        /// </summary>
        /// <value>
        /// The cookies.
        /// </value>
        ICookieCollection Cookies { get; }

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
        /// Gets or sets the protocol version.
        /// </summary>
        /// <value>
        /// The protocol version.
        /// </value>
        Version ProtocolVersion { get; set; }

        /// <summary>
        /// Adds the header.
        /// </summary>
        /// <param name="headerName">Name of the header.</param>
        /// <param name="value">The value.</param>
        void AddHeader(string headerName, string value);

        /// <summary>
        /// Sets the cookie.
        /// </summary>
        /// <param name="sessionCookie">The session cookie.</param>
        void SetCookie(System.Net.Cookie sessionCookie);
    }
}
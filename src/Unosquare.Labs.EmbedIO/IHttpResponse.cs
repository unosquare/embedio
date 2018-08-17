﻿namespace Unosquare.Labs.EmbedIO
{
    using System.IO;
#if NET47
    using System.Net;
#else
    using Net;
#endif

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
        WebHeaderCollection Headers { get; }

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

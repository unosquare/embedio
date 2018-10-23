namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Specialized;

    /// <summary>
    /// Interface to create a HTTP Request/Response.
    /// </summary>
    public interface IHttpBase
    {
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        NameValueCollection Headers { get; }
        
        /// <summary>
        /// Gets the cookies.
        /// </summary>
        /// <value>
        /// The cookies.
        /// </value>
        ICookieCollection Cookies { get; }

        /// <summary>
        /// Gets or sets the protocol version.
        /// </summary>
        /// <value>
        /// The protocol version.
        /// </value>
        Version ProtocolVersion { get; }
    }
}

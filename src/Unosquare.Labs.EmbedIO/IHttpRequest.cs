namespace Unosquare.Labs.EmbedIO
{
    using System.Text;
    using System.IO;
    using System.Collections.Specialized;
    using System;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Interface to create a HTTP Request.
    /// </summary>
    public interface IHttpRequest
    {
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        NameValueCollection Headers { get; }

        /// <summary>
        /// Gets the protocol version.
        /// </summary>
        /// <value>
        /// The protocol version.
        /// </value>
        Version ProtocolVersion { get; }

        /// <summary>
        /// Gets a value indicating whether [keep alive].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [keep alive]; otherwise, <c>false</c>.
        /// </value>
        bool KeepAlive { get; }

        /// <summary>
        /// Gets the cookies.
        /// </summary>
        /// <value>
        /// The cookies.
        /// </value>
        CookieCollection Cookies { get; }

        /// <summary>
        /// Gets the raw URL.
        /// </summary>
        /// <value>
        /// The raw URL.
        /// </value>
        string RawUrl { get; }

        /// <summary>
        /// Gets the query string.
        /// </summary>
        /// <value>
        /// The query string.
        /// </value>
        NameValueCollection QueryString { get; }

        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        /// <value>
        /// The HTTP method.
        /// </value>
        string HttpMethod { get; }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        Uri Url { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has entity body.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has entity body; otherwise, <c>false</c>.
        /// </value>
        bool HasEntityBody { get;  }

        /// <summary>
        /// Gets the input stream.
        /// </summary>
        /// <value>
        /// The input stream.
        /// </value>
        Stream InputStream { get; }

        /// <summary>
        /// Gets the content encoding.
        /// </summary>
        /// <value>
        /// The content encoding.
        /// </value>
        Encoding ContentEncoding { get; }

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        /// <value>
        /// The remote end point.
        /// </value>
        System.Net.IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is local.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is local; otherwise, <c>false</c>.
        /// </value>
        bool IsLocal { get; }

        /// <summary>
        /// Gets the user agent.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        string UserAgent { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is web socket request.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is web socket request; otherwise, <c>false</c>.
        /// </value>
        bool IsWebSocketRequest { get; }

        /// <summary>
        /// Gets the local end point.
        /// </summary>
        /// <value>
        /// The local end point.
        /// </value>
        System.Net.IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>
        /// The type of the content.
        /// </value>
        string ContentType { get; }
    }
}

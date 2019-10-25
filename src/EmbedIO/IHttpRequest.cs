using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace EmbedIO
{
    /// <inheritdoc />
    /// <summary>
    /// Interface to create a HTTP Request.
    /// </summary>
    public interface IHttpRequest : IHttpMessage
    {
        /// <summary>
        /// Gets the request headers.
        /// </summary>
        NameValueCollection Headers { get; }

        /// <summary>
        /// Gets a value indicating whether [keep alive].
        /// </summary>
        bool KeepAlive { get; }

        /// <summary>
        /// Gets the raw URL.
        /// </summary>
        string RawUrl { get; }

        /// <summary>
        /// Gets the query string.
        /// </summary>
        NameValueCollection QueryString { get; }

        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        string HttpMethod { get; }

        /// <summary>
        /// Gets a <see cref="HttpVerbs"/> constant representing the HTTP method of the request.
        /// </summary>
        HttpVerbs HttpVerb { get; }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        Uri Url { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has entity body.
        /// </summary>
        bool HasEntityBody { get;  }

        /// <summary>
        /// Gets the input stream.
        /// </summary>
        Stream InputStream { get; }

        /// <summary>
        /// Gets the content encoding.
        /// </summary>
        Encoding ContentEncoding { get; }

        /// <summary>
        /// Gets the remote end point.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is local.
        /// </summary>
        bool IsLocal { get; }

        /// <summary>
        /// Gets a value indicating whether this request has been received over a SSL connection.
        /// </summary>
        bool IsSecureConnection { get; }

        /// <summary>
        /// Gets the user agent.
        /// </summary>
        string UserAgent { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is web socket request.
        /// </summary>
        bool IsWebSocketRequest { get; }

        /// <summary>
        /// Gets the local end point.
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        string? ContentType { get; }

        /// <summary>
        /// Gets the content length.
        /// </summary>
        long ContentLength64 { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Gets the URL referrer.
        /// </summary>
        Uri? UrlReferrer { get; }
    }
}
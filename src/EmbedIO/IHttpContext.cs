using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using EmbedIO.Sessions;

namespace EmbedIO
{
    /// <summary>
    /// Represents the context of a HTTP(s) request being handled by a web server.
    /// </summary>
    public interface IHttpContext : IMimeTypeProvider
    {
        /// <summary>
        /// Gets a unique identifier for a HTTP context.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the server IP address and port number to which the request is directed.
        /// </summary>
        IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the client IP address and port number from which the request originated.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets the HTTP request.
        /// </summary>
        IHttpRequest Request { get; }

        /// <summary>
        /// Gets the HTTP response object.
        /// </summary>
        IHttpResponse Response { get; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        IPrincipal User { get; }

        /// <summary>
        /// Gets the session proxy associated with this context.
        /// </summary>
        ISessionProxy Session { get; }

        /// <summary>
        /// Gets a value indicating whether compressed request bodies are supported.
        /// </summary>
        /// <seealso cref="WebServerOptionsBase.SupportCompressedRequests"/>
        bool SupportCompressedRequests { get; }

        /// <summary>
        /// Gets the dictionary of data to pass trough the EmbedIO pipeline.
        /// </summary>
        IDictionary<object, object> Items { get; }

        /// <summary>
        /// Registers a callback to be called when processing is finished on a context.
        /// </summary>
        /// <param name="callback">The callback.</param>
        void OnClose(Action<IHttpContext> callback);
    }
}
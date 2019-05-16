using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;

namespace EmbedIO
{
    /// <summary>
    /// Interface to create a HTTP Context.
    /// </summary>
    public interface IHttpContext
    {
        /// <summary>
        /// Gets a unique identifier for a HTTP context.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
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
        /// Gets the HTTP Request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        IHttpRequest Request { get; }

        /// <summary>
        /// Gets the HTTP Response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        IHttpResponse Response { get; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        /// <value>
        /// The user.
        /// </value>
        IPrincipal User { get; }

        /// <summary>
        /// Gets the session proxy associated with this context.
        /// </summary>
        /// <value>
        /// A <see cref="ISessionProxy"/> interface.
        /// </value>
        ISessionProxy Session { get; }

        /// <summary>
        /// Gets or sets the dictionary of data to pass trough the EmbedIO pipeline.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        IDictionary<object, object> Items { get; }

        /// <summary>
        /// Registers a callback to be called when processing is finished on a context.
        /// </summary>
        /// <param name="callback">The callback.</param>
        void OnClose(Action<IHttpContext> callback);
    }
}
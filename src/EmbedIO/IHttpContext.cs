using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;
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
        /// Gets the elapsed time, expressed in milliseconds, since the creation of this context.
        /// </summary>
        long Age { get; }

        /// <summary>
        /// <para>Gets or sets a value indicating whether this <see cref="IHttpContext"/>
        /// has been completely handled, so that no further processing is required.</para>
        /// <para>When a HTTP context is created, this property is <see langword="false" />;
        /// as soon as it is set to <see langword="true" />, the context is not
        /// passed to any further module's handler for processing.</para>
        /// <para>Once set to <see langword="true" />, this property cannot be set
        /// back to <see langword="false" />.</para>
        /// </summary>
        /// <remarks>
        /// <para>When a module's <see cref="IWebModule.IsFinalHandler">PassThrough</see> property is
        /// <see langword="true" />, this property is automatically set to <see langword="true" />
        /// after the <see cref="Task" /> returned by the module's
        /// <see cref="IWebModule.HandleRequestAsync">HandleRequestAsync</see> method
        /// is completed.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">This property is being set to
        /// <see langword="true" /> when its value is <see langword="false" />.</exception>
        bool Handled { get; set; }

        /// <summary>
        /// Registers a callback to be called when processing is finished on a context.
        /// </summary>
        /// <param name="callback">The callback.</param>
        void OnClose(Action<IHttpContext> callback);
    }
}
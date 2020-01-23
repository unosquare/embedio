using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;
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
        /// Gets a <see cref="CancellationToken" /> used to stop processing of this context.
        /// </summary>
        CancellationToken CancellationToken { get; }

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
        /// Gets the route matched by the requested URL path.
        /// </summary>
        RouteMatch Route { get; }

        /// <summary>
        /// Gets the requested path, relative to the innermost module's base path.
        /// </summary>
        /// <remarks>
        /// <para>This property derives from the path specified in the requested URL, stripped of the
        /// <see cref="IWebModule.BaseRoute">BaseRoute</see> of the handling module.</para>
        /// <para>This property is in itself a valid URL path, including an initial
        /// slash (<c>/</c>) character.</para>
        /// </remarks>
        string RequestedPath { get; }

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
        /// <para>Gets a value indicating whether this <see cref="IHttpContext"/>
        /// has been completely handled, so that no further processing is required.</para>
        /// <para>When a HTTP context is created, this property is <see langword="false" />;
        /// as soon as it is set to <see langword="true" />, the context is not
        /// passed to any further module's handler for processing.</para>
        /// <para>Once it becomes <see langword="true" />, this property is guaranteed
        /// to never become <see langword="false" /> again.</para>
        /// </summary>
        /// <remarks>
        /// <para>When a module's <see cref="IWebModule.IsFinalHandler">IsFinalHandler</see> property is
        /// <see langword="true" />, this property is set to <see langword="true" /> after the <see cref="Task" />
        /// returned by the module's <see cref="IWebModule.HandleRequestAsync">HandleRequestAsync</see> method
        /// is completed.</para>
        /// </remarks>
        /// <seealso cref="SetHandled" />
        /// <seealso cref="IWebModule.IsFinalHandler"/>
        bool IsHandled { get; }

        /// <summary>
        /// <para>Marks this context as handled, so that it will not be
        /// processed by any further module.</para>
        /// </summary>
        /// <remarks>
        /// <para>Calling this method from the <see cref="IWebModule.HandleRequestAsync" />
        /// or <see cref="WebModuleBase.OnRequestAsync" /> of a module whose
        /// <see cref="IWebModule.IsFinalHandler" /> property is <see langword="true" />
        /// is redundant and has no effect.</para>
        /// </remarks>
        /// <seealso cref="IsHandled"/>
        /// <seealso cref="IWebModule.IsFinalHandler"/>
        void SetHandled();

        /// <summary>
        /// Registers a callback to be called when processing is finished on a context.
        /// </summary>
        /// <param name="callback">The callback.</param>
        void OnClose(Action<IHttpContext> callback);
    }
}
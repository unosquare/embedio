using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;

namespace EmbedIO
{
    /// <summary>
    /// Represents a module.
    /// </summary>
    public interface IWebModule
    {
        /// <summary>
        /// Gets the base route of a module.
        /// </summary>
        /// <value>
        /// The base route.
        /// </value>
        /// <remarks>
        /// <para>A base route is either "/" (the root path),
        /// or a prefix starting and ending with a '/' character.</para>
        /// </remarks>
        string BaseRoute { get; }

        /// <summary>
        /// Gets a value indicating whether processing of a request should stop
        /// after a module has handled it.
        /// </summary>
        /// <remarks>
        /// <para>If this property is <see langword="true" />, a HTTP context's
        /// <see cref="IHttpContext.SetHandled" /> method will be automatically called
        /// immediately after after the <see cref="Task" /> returned by
        /// <see cref="HandleRequestAsync" /> is completed. This will prevent
        /// the context from being passed further along to other modules.</para>
        /// </remarks>
        /// <seealso cref="IHttpContext.IsHandled" />
        /// <seealso cref="IHttpContext.SetHandled" />
        bool IsFinalHandler { get; }

        /// <summary>
        /// <para>Gets or sets a callback that is called every time an unhandled exception
        /// occurs during the processing of a request.</para>
        /// <para>If this property is <see langword="null"/> (the default),
        /// the exception will be handled by the web server, or by the containing
        /// <see cref="ModuleGroup"/>.</para>
        /// </summary>
        /// <seealso cref="ExceptionHandler"/>
        ExceptionHandlerCallback? OnUnhandledException { get; set; }

        /// <summary>
        /// <para>Gets or sets a callback that is called every time a HTTP exception
        /// is thrown during the processing of a request.</para>
        /// <para>If this property is <see langword="null"/> (the default),
        /// the exception will be handled by the web server, or by the containing
        /// <see cref="ModuleGroup"/>.</para>
        /// </summary>
        /// <seealso cref="HttpExceptionHandler"/>
        HttpExceptionHandlerCallback? OnHttpException { get; set; }

        /// <summary>
        /// Signals a module that the web server is starting.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Matches the specified URL path against a module's <see cref="BaseRoute"/>,
        /// extracting values for the route's parameters and a sub-path.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <returns>If the match is successful, a <see cref="RouteMatch"/> object;
        /// otherwise, <see langword="null"/>.</returns>
        RouteMatch MatchUrlPath(string urlPath);

        /// <summary>
        /// Handles a request from a client.
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        Task HandleRequestAsync(IHttpContext context);
    }
}
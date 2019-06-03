using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// Represents a module.
    /// </summary>
    public interface IWebModule
    {
        /// <summary>
        /// Gets base URL path that a module handles.
        /// </summary>
        /// <value>
        /// The base URL path.
        /// </value>
        /// <remarks>
        /// <para>A base URL path is either "/" (the root path),
        /// or a prefix starting and ending with a '/' character.</para>
        /// </remarks>
        string BaseUrlPath { get; }

        /// <summary>
        /// Signals a module that the web server is starting.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Handles a request from a client.
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="BaseUrlPath"/>. See the Remarks section for more information.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        /// <remarks>
        /// <para>The path specified in the requested URL is stripped of the <see cref="BaseUrlPath"/>
        /// and passed in the <paramref name="path"/> parameter.</para>
        /// <para>The <paramref name="path"/> parameter is in itself a valid URL path, including an initial
        /// slash (<c>/</c>) character.</para>
        /// </remarks>
        Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken);

        /// <summary>
        /// <para>Gets or sets a callback that is called every time an unhandled exception
        /// occurs during the processing of a request.</para>
        /// <para>If this property is <see langword="null"/> (the default),
        /// the exception will be handled by the web server, or by the containing
        /// <see cref="ModuleGroup"/>.</para>
        /// </summary>
        /// <seealso cref="StandardExceptionHandlers"/>
        WebExceptionHandler OnUnhandledException { get; set; }
    }
}

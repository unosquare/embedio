using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard request handler callbacks.
    /// </summary>
    /// <seealso cref="RequestHandlerCallback"/>
    public static class RequestHandler
    {
        /// <summary>
        /// <para>Unconditionally passes a request down the module chain.</para>
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A completed <see cref="Task"/> whose result will be <see langword="false"/>, unless
        /// <paramref name="cancellationToken"/> has been canceled, in which case an <see cref="OperationCanceledException"/>
        /// is thrown.</returns>
        public static Task<bool> PassThrough(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }

        /// <summary>
        /// <para>Unconditionally sends a <c>403 Unauthorized</c> response.</para>
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>This method never returns; it throws a <see cref="HttpException"/> instead.</returns>
        public static Task<bool> Unauthorized(IHttpContext context, string path, CancellationToken cancellationToken)
            => throw HttpException.Unauthorized();

        /// <summary>
        /// <para>Unconditionally sends a <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>This method never returns; it throws a <see cref="HttpException"/> instead.</returns>
        public static Task<bool> NotFound(IHttpContext context, string path, CancellationToken cancellationToken)
            => throw HttpException.NotFound();

        /// <summary>
        /// <para>Unconditionally sends a <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>This method never returns; it throws a <see cref="HttpException"/> instead.</returns>
        public static Task<bool> MethodNotAllowed(IHttpContext context, string path, CancellationToken cancellationToken)
            => throw HttpException.MethodNotAllowed();
    }
}
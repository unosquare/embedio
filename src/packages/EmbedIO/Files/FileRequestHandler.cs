using System.Threading.Tasks;

namespace EmbedIO.Files
{
    /// <summary>
    /// Provides standard handler callbacks for <see cref="FileModule"/>.
    /// </summary>
    /// <seealso cref="FileRequestHandlerCallback"/>
    public static class FileRequestHandler
    {
#pragma warning disable CA1801 // Unused parameters - Must respect FileRequestHandlerCallback signature.
        /// <summary>
        /// <para>Unconditionally passes a request down the module chain.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="info">If the requested path has been successfully mapped to a resource (file or directory), the result of the mapping;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>This method never returns; it throws an exception instead.</returns>
        public static Task PassThrough(IHttpContext context, MappedResourceInfo? info)
            => throw RequestHandler.PassThrough();

        /// <summary>
        /// <para>Unconditionally sends a <c>403 Unauthorized</c> response.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="info">If the requested path has been successfully mapped to a resource (file or directory), the result of the mapping;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>This method never returns; it throws a <see cref="HttpException"/> instead.</returns>
        public static Task ThrowUnauthorized(IHttpContext context, MappedResourceInfo? info)
            => throw HttpException.Unauthorized();

        /// <summary>
        /// <para>Unconditionally sends a <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="info">If the requested path has been successfully mapped to a resource (file or directory), the result of the mapping;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>This method never returns; it throws a <see cref="HttpException"/> instead.</returns>
        public static Task ThrowNotFound(IHttpContext context, MappedResourceInfo? info)
            => throw HttpException.NotFound();

        /// <summary>
        /// <para>Unconditionally sends a <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="info">If the requested path has been successfully mapped to a resource (file or directory), the result of the mapping;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>This method never returns; it throws a <see cref="HttpException"/> instead.</returns>
        public static Task ThrowMethodNotAllowed(IHttpContext context, MappedResourceInfo? info)
            => throw HttpException.MethodNotAllowed();
#pragma warning restore CA1801
    }
}
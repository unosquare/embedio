using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Files
{
    /// <summary>
    /// A callback used to handle a request in <see cref="FileModule"/>.
    /// </summary>
    /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
    /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
    /// <param name="info">If <paramref name="path"/> has been successfully mapped to a resource (file or directory), the result of the mapping;
    /// otherwise, <see langword="null"/>.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will tell whether the request has been handled.</returns>
    public delegate Task<bool> FileRequestHandlerCallback(IHttpContext context, string path, MappedResourceInfo info, CancellationToken cancellationToken);
}
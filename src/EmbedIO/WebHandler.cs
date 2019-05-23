using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// A callback used to handle a request.
    /// </summary>
    /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
    /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will tell whether the request has been handled.</returns>
    public delegate Task<bool> WebHandler(IHttpContext context, string path, CancellationToken ct);
}
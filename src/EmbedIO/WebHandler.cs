using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// Represents a callback used to handle a request.
    /// </summary>
    /// <param name="context">The context of the request.</param>
    /// <param name="path">The requested path, either relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see> if any,
    /// or equal to <paramref name="context"/><c>.Request.Url.AbsolutePath</c>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing whether the web handler actually handled the request.</returns>
    public delegate Task<bool> WebHandler(IHttpContext context, string path, CancellationToken ct);
}
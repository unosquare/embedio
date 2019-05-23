using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// A callback used to handle a routed request.
    /// </summary>
    /// <param name="context">A <see cref="IHttpContext"/> interface representing the context of the request.</param>
    /// <param name="path">The requested path, relative to the innermost containing module's <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
    /// <param name="parameters">A <seealso cref="IReadOnlyDictionary{TKey,TValue}"/> interface representing the resolved route parameters.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing operation, whose result will tell whether the request has been handled.</returns>
    public delegate Task<bool> WebRouteHandler(IHttpContext context, string path, IReadOnlyDictionary<string, string> parameters, CancellationToken ct);
}
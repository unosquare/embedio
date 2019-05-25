using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Base class for callbacks used to handle routed requests.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
    /// <param name="path">The URL path that matched the route..</param>
    /// <param name="parameters">A <seealso cref="IReadOnlyDictionary{TKey,TValue}" /> interface representing the resolved route parameters.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the ongoing operation, whose result will tell whether the request has been handled.
    /// </returns>
    public delegate Task<bool> RoutedHandler<in TContext>(TContext context, string path, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken);
}
using System.Collections.Generic;
using System.Threading;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Base class for callbacks used to handle routed requests synchronously.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
    /// <param name="path">The URL path that matched the route..</param>
    /// <param name="parameters">A <seealso cref="IReadOnlyDictionary{TKey,TValue}" /> interface representing the resolved route parameters.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
    /// <returns><see langword="true"/>if the request has been handled; otherwise, <see langword="false"/>.</returns>
    public delegate bool SyncRoutedHandler<in TContext>(TContext context, string path, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken);
}
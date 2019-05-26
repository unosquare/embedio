using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Base class for callbacks used to handle routed requests.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
    /// <param name="route">The matched route.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task" /> representing the ongoing operation, whose result will tell whether the request has been handled.
    /// </returns>
    /// <seealso cref="RouteMatch"/>
    public delegate Task<bool> RoutedHandler<in TContext>(TContext context, RouteMatch route, CancellationToken cancellationToken);
}
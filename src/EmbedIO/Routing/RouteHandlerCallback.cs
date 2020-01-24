using System.Threading.Tasks;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Base class for callbacks used to handle routed requests.
    /// </summary>
    /// <param name="context">An <see cref="IHttpContext" /> interface representing the context of the request.</param>
    /// <param name="route">The matched route.</param>
    /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
    /// <seealso cref="RouteMatch"/>
    public delegate Task RouteHandlerCallback(IHttpContext context, RouteMatch route);
}
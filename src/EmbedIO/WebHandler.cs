using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// Represents a Web Handler.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the success of the web handler.</returns>
    public delegate Task<bool> WebHandler(IHttpContext context, CancellationToken ct);
}
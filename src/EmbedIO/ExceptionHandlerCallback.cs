using System;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// A callback used to provide information about an unhandled exception occurred while processing a request.
    /// </summary>
    /// <param name="context">A <see cref="IHttpContext" /> interface representing the context of the request.</param>
    /// <param name="path">The URL path requested by the client.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the operation.</param>
    /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
    public delegate Task ExceptionHandlerCallback(IHttpContext context, string path, Exception exception, CancellationToken cancellationToken);
}
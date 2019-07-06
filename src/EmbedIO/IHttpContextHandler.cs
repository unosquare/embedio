using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// <para>Represents an object that can handle a HTTP context.</para>
    /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
    /// </summary>
    public interface IHttpContextHandler
    {
        /// <summary>
        /// <para>Asynchronously handles a HTTP context, generating a suitable response
        /// for an incoming request.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>
        /// used to cancel the  operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        Task HandleContextAsync(IHttpContextImpl context, CancellationToken cancellationToken);
    }
}
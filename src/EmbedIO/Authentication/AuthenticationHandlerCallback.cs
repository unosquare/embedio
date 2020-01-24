using System.Threading.Tasks;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// A callback used to handle events in authentication modules.
    /// </summary>
    /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
    /// <param name="module">The authentication module that called the handler.</param>
    /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
    /// <seealso cref="AuthenticationModuleBase.OnInvalidCredentials"/>
    public delegate Task AuthenticationHandlerCallback(IHttpContext context, AuthenticationModuleBase module);
}
using System.Threading.Tasks;

namespace EmbedIO.Authentication
{
    /// <summary>
    /// Provides standard handler callbacks for authentication modules.
    /// </summary>
    /// <seealso cref="AuthenticationHandlerCallback"/>
    public static class AuthenticationHandler
    {
        /// <summary>
        /// <para>Unconditionally passes a request down the module chain.</para>
        /// <para>In the case of authentication modules, this is accomplished simply by doing nothing,
        /// as the <see cref="IWebModule.IsFinalHandler">IsFinalHandler</see> property of
        /// authentication modules is always set to <see langword="false"/>.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="module">The authentication module that called the handler.</param>
        /// <returns>A completed <see cref="Task"/>.</returns>
#pragma warning disable CA1801 // Unused parameter
        public static Task PassThrough(IHttpContext context, AuthenticationModuleBase module)
#pragma warning restore CA1801
            => Task.CompletedTask;

        /// <summary>
        /// <para>Throws a <see cref="HttpException"/> with a response code of <c>401 Unauthorized</c>.</para>
        /// </summary>
        /// <param name="context">An <see cref="IHttpContext"/> interface representing the context of the request.</param>
        /// <param name="module">The authentication module that called the handler.</param>
        /// <returns>This method never returns; it throws an exception instead..</returns>
        public static Task Unauthorized(IHttpContext context, AuthenticationModuleBase module)
            => throw HttpException.Unauthorized();

        /// <summary>
        /// <para>Throws a <see cref="HttpException"/> with a response code of <c>401 Unauthorized</c>
        /// and, optionally, a custom message and data.</para>
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <param name="data">The data object to include in the response.</param>
        /// <returns>This method never returns; it throws an exception instead..</returns>
        public static AuthenticationHandlerCallback Unauthorized(string? message = null, object? data = null)
            => (context, module) => throw HttpException.Unauthorized(message, data);
    }
}
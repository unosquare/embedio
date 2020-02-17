using System;
using EmbedIO.Internal;

namespace EmbedIO
{
    /// <summary>
    /// Provides standard request handler callbacks.
    /// </summary>
    /// <seealso cref="RequestHandlerCallback"/>
    public static class RequestHandler
    {
        /// <summary>
        /// <para>Returns an exception object that, when thrown from a module's
        /// <see cref="IWebModule.HandleRequestAsync">HandleRequestAsync</see> method, will cause the HTTP context
        /// to be passed down along the module chain, regardless of the value of the module's
        /// <see cref="IWebModule.IsFinalHandler">IsFinalHandler</see> property.</para>
        /// </summary>
        /// <returns>A newly-created <see cref="Exception"/>.</returns>
        public static Exception PassThrough() => new RequestHandlerPassThroughException();

        /// <summary>
        /// <para>Returns a <see cref="RequestHandlerCallback" /> that unconditionally sends a <c>401 Unauthorized</c> response.</para>
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <returns>A <see cref="RequestHandlerCallback" />.</returns>
        public static RequestHandlerCallback ThrowUnauthorized(string? message = null)
            => _ => throw HttpException.Unauthorized(message);

        /// <summary>
        /// <para>Returns a <see cref="RequestHandlerCallback" /> that unconditionally sends a <c>403 Forbidden</c> response.</para>
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <returns>A <see cref="RequestHandlerCallback" />.</returns>
        public static RequestHandlerCallback ThrowForbidden(string? message = null)
            => _ => throw HttpException.Forbidden(message);

        /// <summary>
        /// <para>Returns a <see cref="RequestHandlerCallback" /> that unconditionally sends a <c>400 Bad Request</c> response.</para>
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <returns>A <see cref="RequestHandlerCallback" />.</returns>
        public static RequestHandlerCallback ThrowBadRequest(string? message = null)
            => _ => throw HttpException.BadRequest(message);

        /// <summary>
        /// <para>Returns a <see cref="RequestHandlerCallback" /> that unconditionally sends a <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <returns>A <see cref="RequestHandlerCallback" />.</returns>
        public static RequestHandlerCallback ThrowNotFound(string? message = null)
            => _ => throw HttpException.NotFound(message);

        /// <summary>
        /// <para>Returns a <see cref="RequestHandlerCallback" /> that unconditionally sends a <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="message">A message to include in the response.</param>
        /// <returns>A <see cref="RequestHandlerCallback" />.</returns>
        public static RequestHandlerCallback ThrowMethodNotAllowed(string? message = null)
            => _ => throw HttpException.MethodNotAllowed(message);
    }
}
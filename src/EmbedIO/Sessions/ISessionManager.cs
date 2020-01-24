using System.Threading;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// Represents a session manager, which is in charge of managing session objects
    /// and their association to HTTP contexts.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Signals a session manager that the web server is starting.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to stop the web server.</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the session associated with an <see cref="IHttpContext"/>.
        /// If a session ID can be retrieved for the context and stored session data
        /// are available, the returned <see cref="ISession"/> will contain those data;
        /// otherwise, a new session is created and its ID is stored in the response
        /// to be retrieved by subsequent requests.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>An <see cref="ISession"/> interface.</returns>
        ISession Create(IHttpContext context);

        /// <summary>
        /// Deletes the session (if any) associated with the specified context
        /// and removes the session's ID from the context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="id">The unique ID of the session.</param>
        /// <seealso cref="ISession.Id"/>
        void Delete(IHttpContext context, string id);

        /// <summary>
        /// <para>Called by a session proxy when a session has been obtained
        /// for an <see cref="IHttpContext"/> and the context is closed,
        /// even if the session was subsequently deleted.</para>
        /// <para>This method can be used to save session data to a storage medium.</para>
        /// </summary>
        /// <param name="context">The <see cref="IHttpContext"/> for which a session was obtained.</param>
        void OnContextClose(IHttpContext context);
    }
}
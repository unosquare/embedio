namespace EmbedIO
{
    /// <summary>
    /// Represents the session manager for a web server.
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Gets a session object for the given server context.
        /// If no session exists for the context, <see langword="null"/> is returned.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="SessionInfo"/> object for the given server context.</returns>
        SessionInfo GetSession(IHttpContext context);

        /// <summary>
        /// Gets a session object for the given WebSocket context.
        /// If no session exists for the context, <see langword="null"/> is returned.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A <see cref="SessionInfo"/> object for the given server context.</returns>
        SessionInfo GetSession(IWebSocketContext context);

        /// <summary>
        /// Delete the session object for the given context
        /// If no session exists for the context, then null is returned.
        /// </summary>
        /// <param name="context">The context.</param>
        void DeleteSession(IHttpContext context);

        /// <summary>
        /// Delete a session for the given session info
        /// No exceptions are thrown if the session is not found.
        /// </summary>
        /// <param name="session">The session info.</param>
        void DeleteSession(SessionInfo session);
    }
}
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
#if NET47
    using System.Net.WebSockets;
#else
    using Net;
#endif

    /// <inheritdoc />
    /// <summary>
    /// Interface to create session modules.
    /// </summary>
    public interface ISessionWebModule : IWebModule
    {
        /// <summary>
        /// The dictionary holding the sessions
        /// Direct access is guaranteed to be thread-safe.
        /// </summary>
        /// <value>
        /// The sessions.
        /// </value>
        IReadOnlyDictionary<string, SessionInfo> Sessions { get; }

        /// <summary>
        /// Gets or sets the expiration time for the sessions.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        TimeSpan Expiration { get; set; }

        /// <summary>
        /// Gets a session object for the given server context.
        /// If no session exists for the context, then null is returned.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A session info for the given server context.</returns>
        SessionInfo GetSession(IHttpContext context);

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

        /// <summary>
        /// Gets a session object for the given WebSocket context.
        /// If no session exists for the context, then null is returned.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A session object for the given WebSocket context.</returns>
        SessionInfo GetSession(WebSocketContext context);
    }
}
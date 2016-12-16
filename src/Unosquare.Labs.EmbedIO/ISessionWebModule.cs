namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
#if NET46
    using System.Net;
    using System.Net.WebSockets;
#else
    using Net;
#endif

    /// <summary>
    /// Interface to create session modules
    /// </summary>
    public interface ISessionWebModule : IWebModule
    {
        /// <summary>
        /// The dictionary holding the sessions
        /// Direct manipulation is not guaranteed to be thread-safe
        /// </summary>
        /// <value>
        /// The sessions.
        /// </value>
        IDictionary<string, SessionInfo> Sessions { get; }

        /// <summary>
        /// Gets a session object for the given server context.
        /// If no session exists for the context, then null is returned
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        SessionInfo GetSession(HttpListenerContext context);

        /// <summary>
        /// Delete the session object for the given context
        /// If no session exists for the context, then null is returned
        /// </summary>
        void DeleteSession(HttpListenerContext context);

        /// <summary>
        /// Delete a session for the given session info
        /// No exceptions are thrown if the session is not found
        /// </summary>
        /// <param name="session">The session info.</param>
        void DeleteSession(SessionInfo session);

        /// <summary>
        /// Gets a session object for the given WebSocket context.
        /// If no session exists for the context, then null is returned
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        SessionInfo GetSession(WebSocketContext context);

        /// <summary>
        /// Gets or sets the expiration time for the sessions.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        TimeSpan Expiration { get; set; }
    }
}

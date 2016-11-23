namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Concurrent;
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
        /// The concurrent dictionary holding the sessions
        /// </summary>
        /// <value>
        /// The sessions.
        /// </value>
        ConcurrentDictionary<string, SessionInfo> Sessions { get; }

        /// <summary>
        /// Gets a session object for the given server context.
        /// If no session exists for the context, then null is returned
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        SessionInfo GetSession(HttpListenerContext context);

        /// <summary>
        /// Delete the session object for the given context
        /// </summary>
        void DeleteSession(HttpListenerContext context);

        /// <summary>
        /// Delete a session for the given session info
        /// </summary>
        /// <param name="session">The session info.</param>
        void DeleteSession(SessionInfo session);
        
        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        SessionInfo GetSession(WebSocketContext context);

        /// <summary>
        /// Gets or sets the expiration.
        /// </summary>
        /// <value>
        /// The expiration.
        /// </value>
        TimeSpan Expiration { get; set; }
    }
}

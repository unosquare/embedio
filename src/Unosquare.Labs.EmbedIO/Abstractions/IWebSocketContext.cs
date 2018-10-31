namespace Unosquare.Labs.EmbedIO
{
    using System;

    /// <summary>
    /// Interface to create a WebSocket Context.
    /// </summary>
    public interface IWebSocketContext
    {
        /// <summary>
        /// Gets or sets the web socket.
        /// </summary>
        /// <value>
        /// The web socket.
        /// </value>
        IWebSocket WebSocket { get; }

        /// <summary>
        /// Gets the cookie collection.
        /// </summary>
        /// <value>
        /// The cookie collection.
        /// </value>
        ICookieCollection CookieCollection { get; }

        /// <summary>
        /// Gets the request URI.
        /// </summary>
        /// <value>
        /// The request URI.
        /// </value>
        Uri RequestUri { get; }
    }
}

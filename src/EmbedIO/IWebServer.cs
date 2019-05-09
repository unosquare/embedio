﻿using EmbedIO.Constants;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO
{
    /// <summary>
    /// <para>Represents a web server.</para>
    /// <para>The basic usage of a web server is as follows:</para>
    /// <list type="bullet">
    /// <item><description>add modules to the <see cref="IWebModuleContainer.Modules">Modules</see> collection;</description></item>
    /// <item><description>set a <see cref="SessionManager"/> if needed;</description></item>
    /// <item><description>call <see cref="RunAsync"/> to respond to incoming requests.</description></item>
    /// </list>
    /// </summary>
    public interface IWebServer : IWebModuleContainer
    {
        /// <summary>
        /// Occurs when the <see cref="State"/> property changes.
        /// </summary>
        event WebServerStateChangedEventHandler StateChanged;

        /// <summary>
        /// <para>Gets the registered session manager, if any.</para>
        /// <para>A session manager is an implementation of <see cref="ISessionManager"/>
        /// to handle session data.</para>
        /// </summary>  
        /// <value>
        /// The session manager, or <see langword="null"/> if no session manager is present.
        /// </value>
        ISessionManager SessionManager { get; }

        /// <summary>
        /// Gets the state of the web server.
        /// </summary>
        /// <value>The state.</value>
        /// <seealso cref="WebServerState"/>
        WebServerState State { get; }

        /// <summary>
        /// Starts the listener and the registered modules.
        /// </summary>
        /// <param name="ct">The cancellation token; when cancelled, the server cancels all pending requests and stops.</param>
        /// <returns>
        /// Returns the task that the HTTP listener is running inside of, so that it can be waited upon after it's been canceled.
        /// </returns>
        Task RunAsync(CancellationToken ct = default);
    }
}
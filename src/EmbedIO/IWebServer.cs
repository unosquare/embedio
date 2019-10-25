using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Sessions;

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
    public interface IWebServer : IWebModuleContainer, IMimeTypeCustomizer
    {
        /// <summary>
        /// Occurs when the <see cref="State"/> property changes.
        /// </summary>
        event WebServerStateChangedEventHandler StateChanged;

        /// <summary>
        /// <para>Gets or sets a callback that is called every time an unhandled exception
        /// occurs during the processing of a request.</para>
        /// <para>This property can never be <see langword="null"/>.
        /// If it is still </para>
        /// </summary>
        /// <seealso cref="ExceptionHandler"/>
        ExceptionHandlerCallback OnUnhandledException { get; set; }

        /// <summary>
        /// <para>Gets or sets a callback that is called every time a HTTP exception
        /// is thrown during the processing of a request.</para>
        /// <para>This property can never be <see langword="null"/>.</para>
        /// </summary>
        /// <seealso cref="HttpExceptionHandler"/>
        HttpExceptionHandlerCallback OnHttpException { get; set; }

        /// <summary>
        /// <para>Gets or sets the registered session ID manager, if any.</para>
        /// <para>A session ID manager is an implementation of <see cref="ISessionManager"/>.</para>
        /// <para>Note that this property can only be set before starting the web server.</para>
        /// </summary>  
        /// <value>
        /// The session manager, or <see langword="null"/> if no session manager is present.
        /// </value>
        /// <exception cref="InvalidOperationException">This property is being set and the web server has already been started.</exception>
        ISessionManager? SessionManager { get; set; }

        /// <summary>
        /// Gets the state of the web server.
        /// </summary>
        /// <value>The state.</value>
        /// <seealso cref="WebServerState"/>
        WebServerState State { get; }

        /// <summary>
        /// Starts the listener and the registered modules.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token; when cancelled, the server cancels all pending requests and stops.</param>
        /// <returns>
        /// Returns the task that the HTTP listener is running inside of, so that it can be waited upon after it's been canceled.
        /// </returns>
        Task RunAsync(CancellationToken cancellationToken = default);
    }
}
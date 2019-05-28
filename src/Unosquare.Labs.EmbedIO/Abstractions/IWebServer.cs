namespace Unosquare.Labs.EmbedIO
{
    using System;
    using Core;
    using Constants;
    using System.Threading;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface to create a WebServer class.
    ///
    /// The basic behaviour for a WebServer is register/unregister modules and
    /// run asynchronous to receive incoming HTTP Requests.
    /// </summary>
    public interface IWebServer
    {
        /// <summary>
        /// Occurs when [state changed].
        /// </summary>
        event WebServerStateChangedEventHandler StateChanged;

        /// <summary>
        /// Gets registered SessionModule (if any).
        ///
        /// SessionModule is an implementation of <c>ISessionModule</c>
        /// to handle session data.
        /// </summary>  
        /// <value>
        /// The session module.
        /// </value>
        ISessionWebModule SessionModule { get; }

        /// <summary>
        /// Gets the URL RoutingStrategy used in this instance.
        /// 
        /// By default it is set to Wildcard, but Regex is the recommended value.
        /// </summary>
        /// <value>
        /// The routing strategy.
        /// </value>
        RoutingStrategy RoutingStrategy { get; }

        /// <summary>
        /// Gets a list of registered modules.
        /// </summary>
        /// <value>
        /// The modules.
        /// </value>
        ReadOnlyCollection<IWebModule> Modules { get; }

        /// <summary>
        /// Gets or sets the on method not allowed.
        /// </summary>
        /// <value>
        /// The on method not allowed.
        /// </value>
        [Obsolete("OnMethodNotAllowed will be dropped in future versions")]
        Func<IHttpContext, Task<bool>> OnMethodNotAllowed { get; set; }

        /// <summary>
        /// Gets or sets the on not found.
        /// </summary>
        /// <value>
        /// The on not found.
        /// </value>
        [Obsolete("OnNotFound will be dropped in future versions")]
        Func<IHttpContext, Task<bool>> OnNotFound { get; set; }

        /// <summary>
        /// Gets or sets the unhandled exception.
        /// </summary>
        /// <value>
        /// The unhandled exception.
        /// </value>
        [Obsolete("UnhandledException will be dropped in future versions")]
        Func<IHttpContext, Exception, CancellationToken, Task<bool>> UnhandledException { get; set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        WebServerState State { get; }

        /// <summary>
        /// Gets the module registered for the given type.
        /// Returns null if no module matches the given type.
        /// </summary>
        /// <typeparam name="T">The type of module.</typeparam>
        /// <returns>Module registered for the given type.</returns>
        T Module<T>()
            where T : class, IWebModule;

        /// <summary>
        /// Registers an instance of a web module. Only 1 instance per type is allowed.
        /// </summary>
        /// <param name="webModule">The module.</param>
        void RegisterModule(IWebModule webModule);

        /// <summary>
        /// Unregisters the module identified by its type.
        /// </summary>
        /// <param name="moduleType">Type of the module.</param>
        void UnregisterModule(Type moduleType);

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

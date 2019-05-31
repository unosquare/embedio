namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System;
    using System.Threading;

    /// <summary>
    /// Interface to create web modules.
    /// </summary>
    public interface IWebModule
    {
        /// <summary>
        /// Gets the friendly name of the module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [Obsolete("Name will be dropped in future versions")]
        string Name { get; }

        /// <summary>
        /// Gets the registered handlers.
        /// </summary>
        /// <value>
        /// The handlers.
        /// </value>
        [Obsolete("Server will be dropped in future versions")]
        ModuleMap Handlers { get; }

        /// <summary>
        /// Gets the associated Web Server object.
        /// This property is automatically set when the module is registered.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        [Obsolete("Server will be dropped in future versions")]
        IWebServer Server { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is watchdog enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is watchdog enabled; otherwise, <c>false</c>.
        /// </value>
        [Obsolete("Watchdog will be dropped in future versions")]
        bool IsWatchdogEnabled { get; set; }

        /// <summary>
        /// Gets or sets the watchdog interval.
        /// </summary>
        /// <value>
        /// The watchdog interval.
        /// </value>
        [Obsolete("Watchdog will be dropped in future versions")]
        TimeSpan WatchdogInterval { get; set; }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        /// <value>
        /// The cancellation token.
        /// </value>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds a handler that gets called when a path and verb are matched.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="handler">The handler.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// handler.
        /// </exception>
        [Obsolete("WebHandler will be dropped in future versions")]
        void AddHandler(string path, HttpVerbs verb, WebHandler handler);

        /// <summary>
        /// Starts the Web Module.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        void Start(CancellationToken ct);

        /// <summary>
        /// Runs the watchdog.
        /// </summary>
        [Obsolete("Watchdog will be dropped in future versions")]
        void RunWatchdog();
    }
}

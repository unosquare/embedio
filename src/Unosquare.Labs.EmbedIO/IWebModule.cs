namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

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
        string Name { get; }

        /// <summary>
        /// Gets the registered handlers.
        /// </summary>
        /// <value>
        /// The handlers.
        /// </value>
        ModuleMap Handlers { get; }

        /// <summary>
        /// Gets the associated Web Server object.
        /// This property is automatically set when the module is registered.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        IWebServer Server { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is watchdog enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is watchdog enabled; otherwise, <c>false</c>.
        /// </value>
        bool IsWatchdogEnabled { get; set; }

        /// <summary>
        /// Gets or sets the watchdog interval.
        /// </summary>
        /// <value>
        /// The watchdog interval.
        /// </value>
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
        void AddHandler(string path, HttpVerbs verb, Func<IHttpContext, CancellationToken, Task<bool>> handler);

        /// <summary>
        /// Starts the Web Module.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        void Start(CancellationToken ct);

        /// <summary>
        /// Runs the watchdog.
        /// </summary>
        void RunWatchdog();
    }
}

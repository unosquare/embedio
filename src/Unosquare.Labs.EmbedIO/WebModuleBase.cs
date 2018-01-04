namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using System;
    using System.Threading;
    using System.Threading.Tasks;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Base class to define custom web modules
    /// inherit from this class and use the AddHandler Method to register your method calls
    /// </summary>
    public abstract class WebModuleBase : IWebModule
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebModuleBase"/> class.
        /// </summary>
        protected WebModuleBase()
        {
            Handlers = new ModuleMap();

            var watchDogTask = Task.Factory.StartNew(async () =>
            {
                RunWatchdog();
                await Task.Delay(WatchdogInterval, _cts.Token);
            }, _cts.Token);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WebModuleBase"/> class.
        /// </summary>
        ~WebModuleBase()
        {
            _cts.Cancel();
        }

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the registered handlers.
        /// Use the AddHandler method to register Handlers
        /// </summary>
        /// <value>
        /// The handlers.
        /// </value>
        public ModuleMap Handlers { get; protected set; }

        /// <summary>
        /// Gets the associated Web Server object.
        /// This property is automatically set when the module is registered
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public WebServer Server { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is watchdog enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is watchdog enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsWatchdogEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the watchdog interval.
        /// </summary>
        /// <value>
        /// The watchdog interval.
        /// </value>
        public TimeSpan WatchdogInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Adds a method handler for a given path and verb
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="handler">The handler.</param>
        /// <exception cref="System.ArgumentNullException">
        /// path
        /// or
        /// handler
        /// </exception>
        public void AddHandler(string path, HttpVerbs verb, Func<HttpListenerContext, CancellationToken, Task<bool>> handler)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            
            Handlers.Add(new Map { Path = path, Verb = verb, ResponseHandler = handler});
        }

        /// <summary>
        /// Runs the watchdog.
        /// </summary>
        public virtual void RunWatchdog()
        {
            // do nothing
        }
    }
}
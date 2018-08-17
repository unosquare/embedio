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
    /// Base class to define custom web modules.
    /// Inherit from this class and use the AddHandler Method to register your method calls.
    /// </summary>
    public abstract class WebModuleBase 
        : IWebModule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebModuleBase"/> class.
        /// </summary>
        protected WebModuleBase()
        {
            Handlers = new ModuleMap();
        }

        /// <inheritdoc/>
        public abstract string Name { get; }

        /// <inheritdoc/>
        public ModuleMap Handlers { get; protected set; }
        
        /// <inheritdoc/>
        public IWebServer Server { get; set; }
        
        /// <inheritdoc/>
        public bool IsWatchdogEnabled { get; set; }
        
        /// <inheritdoc/>
        public TimeSpan WatchdogInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <inheritdoc />
        public CancellationToken CancellationToken { get; protected set; }
        
        /// <inheritdoc/>
        public void AddHandler(string path, HttpVerbs verb, Func<HttpListenerContext, CancellationToken, Task<bool>> handler)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Handlers.Add(new Map {Path = path, Verb = verb, ResponseHandler = handler});
        }

        /// <inheritdoc />
        public void Start(CancellationToken ct)
        {
            CancellationToken = ct;

            var watchDogTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        RunWatchdog();
                        await Task.Delay(WatchdogInterval, ct);
                    }
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
            }, ct);
        }
        
        /// <inheritdoc/>
        public virtual void RunWatchdog()
        {
            // do nothing
        }
    }
}
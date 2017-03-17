namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
#if NET46
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
        /// Initializes a new instance of the <see cref="WebModuleBase"/> class.
        /// </summary>
        protected WebModuleBase()
        {
            Handlers = new ModuleMap();
        }

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

            Handlers.Add(new Map {Path = path, Verb = verb, ResponseHandler = handler});
        }
    }
}
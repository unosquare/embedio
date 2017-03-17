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
    /// Interface to create web modules
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
        /// Gets the handlers.
        /// </summary>
        /// <value>
        /// The handlers.
        /// </value>
        ModuleMap Handlers { get; }

        /// <summary>
        /// Adds a handler that gets called when a path and verb are matched.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="handler">The handler.</param>
        void AddHandler(string path, HttpVerbs verb, Func<HttpListenerContext, CancellationToken, Task<bool>> handler);

        /// <summary>
        /// Gets the server owning this module. This property is set automatically after registering the module.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        WebServer Server { get; set; }
    }
}

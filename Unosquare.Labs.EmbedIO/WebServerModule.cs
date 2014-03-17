namespace Unosquare.Labs.EmbedIO
{
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// Base class to define custom web modules
    /// inherit from this class and use the AddHandler Method to register you method calls
    /// </summary>
    public abstract class WebServerModule
    {
        /// <summary>
        /// A delegate that handles certain action in a module given a path and a verb
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public delegate bool ResponseHandler(WebServer server, HttpListenerContext context);

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
        public WebServerModuleMap Handlers { get; protected set; }

        /// <summary>
        /// Gets the associated Web Server object.
        /// This property is automatically set when the server passes a request to the module
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public WebServer Server { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerModule"/> class.
        /// </summary>
        protected WebServerModule()
        {
            this.Handlers = new WebServerModuleMap();
        }

        /// <summary>
        /// Adds a method handler for a given path and verb
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="handler">The handler.</param>
        public void AddHandler(string path, HttpVerbs verb, ResponseHandler handler)
        {
            this.Handlers[path] = new Dictionary<HttpVerbs, ResponseHandler>() { { verb, handler } };
        }

    }

}

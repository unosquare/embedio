namespace Unosquare.Labs.EmbedIO
{
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// A delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public delegate bool ResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// An async delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public delegate Task<bool> AsyncResponseHandler(WebServer server, HttpListenerContext context);

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
        void AddHandler(string path, HttpVerbs verb, ResponseHandler handler);

        /// <summary>
        /// Gets the server owning this module. This property is set automatically after registering the module.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        WebServer Server { get; set; }
    }
}

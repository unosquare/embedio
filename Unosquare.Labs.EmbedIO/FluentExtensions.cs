namespace Unosquare.Labs.EmbedIO
{
    using System.Linq;
    using System.Reflection;
    using Unosquare.Labs.EmbedIO.Modules;

    /// <summary>
    /// Extensions methods to Fluent Interface
    /// </summary>
    public static class FluentExtensions
    {
        /// <summary>
        /// Add StaticFilesModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="rootPath">The static folder path.</param>
        /// <param name="defaultDocument">The default document name</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer WithStaticFolderAt(this WebServer webserver, string rootPath,
            string defaultDocument = StaticFilesModule.DefaultDocumentName)
        {
            webserver.RegisterModule(new StaticFilesModule(rootPath) {DefaultDocument = defaultDocument});
            return webserver;
        }

        /// <summary>
        /// Add StaticFilesModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <returns></returns>
        public static WebServer WithLocalSession(this WebServer webserver)
        {
            webserver.RegisterModule(new LocalSessionModule());
            return webserver;
        }

        /// <summary>
        /// Add WebApiModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="autoload">Set if autload WebApi Controllers should run</param>
        /// <param name="assembly">The assembly to load WebApi Controllers</param>
        /// <param name="verbose">Set verbose</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer WithWebApi(this WebServer webserver, bool autoload = false, Assembly assembly = null,
            bool verbose = false)
        {
            webserver.RegisterModule(new WebApiModule());

            return autoload ? webserver.LoadApiControllers(assembly) : webserver;
        }

        /// <summary>
        /// Add WebSocketsModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="autoload">Set if autload Web Sockets should run</param>
        /// <param name="assembly">The assembly to load Web Sockets</param>
        /// <param name="verbose">Set verbose</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer WithWebSocket(this WebServer webserver, bool autoload = false, Assembly assembly = null,
            bool verbose = false)
        {
            webserver.RegisterModule(new WebSocketsModule());

            return autoload ? webserver.LoadWebSockets(assembly) : webserver;
        }

        /// <summary>
        /// Load all the WebApi Controllers in an assembly
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers</param>
        /// <param name="verbose">Set verbose</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer LoadApiControllers(this WebServer webserver, Assembly assembly = null,
            bool verbose = false)
        {
            var types = (assembly ?? Assembly.GetExecutingAssembly()).GetTypes();

            var apiControllers =
                types.Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof (WebApiController))).ToArray();

            if (apiControllers.Any())
            {
                foreach (var apiController in apiControllers)
                {
                    if (webserver.Module<WebApiModule>() == null) webserver = webserver.WithWebApi();

                    webserver.Module<WebApiModule>().RegisterController(apiController);
                    if (verbose) webserver.Log.DebugFormat("Registering {0} WebAPI", apiController.Name);
                }
            }

            return webserver;
        }

        /// <summary>
        /// Load all the WebSockets in an assembly
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebSockets</param>
        /// <param name="verbose">Set verbose</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer LoadWebSockets(this WebServer webserver, Assembly assembly = null, bool verbose = false)
        {
            var types = (assembly ?? Assembly.GetExecutingAssembly()).GetTypes();

            var sockerServers =
                types.Where(x => x.BaseType == typeof (WebSocketsServer)).ToArray();

            if (sockerServers.Any())
            {
                foreach (var socketServer in sockerServers)
                {
                    if (webserver.Module<WebSocketsModule>() == null) webserver = webserver.WithWebSocket();

                    webserver.Module<WebSocketsModule>().RegisterWebSocketsServer(socketServer);
                    if (verbose) webserver.Log.DebugFormat("Registering {0} WebSocket", socketServer.Name);
                }
            }

            return webserver;
        }
    }
}
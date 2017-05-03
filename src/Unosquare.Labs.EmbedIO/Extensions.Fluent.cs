namespace Unosquare.Labs.EmbedIO
{
    using Modules;
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Extensions methods to EmbedIO's Fluent Interface
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Add the StaticFilesModule to the specified WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="rootPath">The static folder path.</param>
        /// <param name="defaultDocument">The default document name</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="System.ArgumentException">Argument cannot be null.;webserver</exception>
        public static WebServer WithStaticFolderAt(this WebServer webserver, string rootPath,
            string defaultDocument = StaticFilesModule.DefaultDocumentName)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            webserver.RegisterModule(new StaticFilesModule(rootPath) { DefaultDocument = defaultDocument });
            return webserver;
        }

        /// <summary>
        /// Add the StaticFilesModule with Multiple paths
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="virtualPaths">The virtual paths.</param>
        /// <param name="defaultDocument">The default document.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static WebServer WithVirtualPaths(this WebServer webserver, Dictionary<string, string> virtualPaths,
            string defaultDocument = StaticFilesModule.DefaultDocumentName)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            webserver.RegisterModule(new StaticFilesModule(virtualPaths) { DefaultDocument = defaultDocument });
            return webserver;
        }

        /// <summary>
        /// Add StaticFilesModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <returns></returns>
        public static WebServer WithLocalSession(this WebServer webserver)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            webserver.RegisterModule(new LocalSessionModule());
            return webserver;
        }

#if !NETSTANDARD1_3 && !UWP
        /// <summary>
        /// Add WebApiModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers from. Leave null to avoid autoloading.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer WithWebApi(this WebServer webserver, Assembly assembly = null)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            webserver.RegisterModule(new WebApiModule());
            return (assembly != null) ? webserver.LoadApiControllers(assembly) : webserver;
        }

        /// <summary>
        /// Add WebSocketsModule to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load Web Sockets from. Leave null to avoid autoloading.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer WithWebSocket(this WebServer webserver, Assembly assembly = null)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            webserver.RegisterModule(new WebSocketsModule());
            return (assembly != null) ? webserver.LoadWebSockets(assembly) : webserver;
        }

        /// <summary>
        /// Load all the WebApi Controllers in an assembly
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers from. Leave null to load from the currently executing assembly.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer LoadApiControllers(this WebServer webserver, Assembly assembly = null)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            var types = (assembly ?? Assembly.GetEntryAssembly()).GetTypes();
            var apiControllers =
                types.Where(x => x.GetTypeInfo().IsClass
                                 && !x.GetTypeInfo().IsAbstract
                                 && x.GetTypeInfo().IsSubclassOf(typeof(WebApiController))).ToArray();

            if (apiControllers.Any())
            {
                foreach (var apiController in apiControllers)
                {
                    if (webserver.Module<WebApiModule>() == null) webserver = webserver.WithWebApi();

                    webserver.Module<WebApiModule>().RegisterController(apiController);
                    $"Registering WebAPI Controller '{apiController.Name}'".Debug();
                }
            }

            return webserver;
        }

        /// <summary>
        /// Load all the WebApi Controllers in an assembly
        /// </summary>
        /// <param name="apiModule">The apíModule instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers from. Leave null to load from the currently executing assembly.</param>
        /// <returns>The webserver instance.</returns>
        public static WebApiModule LoadApiControllers(this WebApiModule apiModule, Assembly assembly = null)
        {
            if (apiModule == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(apiModule));

            var types = (assembly ?? Assembly.GetEntryAssembly()).GetTypes();
            var apiControllers =
                types.Where(x => x.GetTypeInfo().IsClass
                                 && !x.GetTypeInfo().IsAbstract
                                 && x.GetTypeInfo().IsSubclassOf(typeof(WebApiController))).ToArray();

            if (apiControllers.Any())
            {
                foreach (var apiController in apiControllers)
                {
                    apiModule.RegisterController(apiController);
                }
            }

            return apiModule;
        }

        /// <summary>
        /// Load all the WebSockets in an assembly
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebSocketsServer types from. Leave null to load from the currently executing assembly.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer LoadWebSockets(this WebServer webserver, Assembly assembly = null)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            var types = (assembly ?? Assembly.GetEntryAssembly()).GetTypes();
            var sockerServers =
                types.Where(x => x.GetTypeInfo().BaseType == typeof(WebSocketsServer)).ToArray();

            if (sockerServers.Any())
            {
                foreach (var socketServer in sockerServers)
                {
                    if (webserver.Module<WebSocketsModule>() == null) webserver = webserver.WithWebSocket();

                    webserver.Module<WebSocketsModule>().RegisterWebSocketsServer(socketServer);
                    $"Registering WebSocket Server '{socketServer.Name}'".Info();
                }
            }

            return webserver;
        }
#endif

        /// <summary>
        /// Enables CORS in the WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="origins">The valid origins, default all</param>
        /// <param name="headers">The valid headers, default all</param>
        /// <param name="methods">The valid method, default all</param>
        /// <returns></returns>
        public static WebServer EnableCors(this WebServer webserver, string origins = Constants.CorsWildcard,
            string headers = Constants.CorsWildcard,
            string methods = Constants.CorsWildcard)
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            webserver.RegisterModule(new CorsModule(origins, headers, methods));

            return webserver;
        }

        /// <summary>
        /// Add WebApi Controller to WebServer
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <returns>The webserver instance.</returns>
        public static WebServer WithWebApiController<T>(this WebServer webserver) where T : WebApiController, new()
        {
            if (webserver == null)
                throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(webserver));

            if (webserver.Module<WebApiModule>() == null)
            {
                webserver.RegisterModule(new WebApiModule());
            }

            webserver.Module<WebApiModule>().RegisterController<T>();

            return webserver;
        }
    }
}
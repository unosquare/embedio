using System;
using System.Linq;
using System.Reflection;
using EmbedIO.Modules;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO
{
    /// <summary>
    /// Extensions methods to EmbedIO's Fluent Interface.
    /// </summary>
    public static partial class Extensions
    {
        /// <summary>
        /// Add StaticFilesModule to WebServer.
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <returns>An instance of a web module.</returns>
        /// <exception cref="System.ArgumentNullException">webserver.</exception>
        public static IWebServer WithLocalSession(this IWebServer webserver)
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            webserver.Modules.Add(new LocalSessionManager());
            return webserver;
        }
        
        /// <summary>
        /// Add WebApiModule to WebServer.
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers from. Leave null to avoid auto-loading.</param>
        /// <param name="responseJsonException">if set to <c>true</c> [response json exception].</param>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <returns>
        /// An instance of webserver.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer WithWebApi(
            this IWebServer webserver, 
            Assembly assembly = null,
            bool responseJsonException = false,
            string baseUrlPath = UrlPath.Root)
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            var webApiModule = new WebApiModule(baseUrlPath, responseJsonException);
            
            if (assembly != null)
                webApiModule.LoadApiControllers(assembly);
            
            webserver.Modules.Add(webApiModule);

            return webserver;
        }

        /// <summary>
        /// Add WebSocketsModule to WebServer.
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load Web Sockets from. Leave null to avoid auto-loading.</param>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <returns>
        /// An instance of webserver.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver</exception>
        /// <exception cref="System.ArgumentNullException">webserver.</exception>
        public static IWebServer WithWebSocket(
            this IWebServer webserver, 
            Assembly assembly = null,
            string baseUrlPath = UrlPath.Root)
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            webserver.Modules.Add(new WebSocketsModule(baseUrlPath));
            return assembly != null ? webserver.LoadWebSockets(assembly) : webserver;
        }

        /// <summary>
        /// Load all the WebApi Controllers in an assembly.
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers from. Leave null to load from the currently executing assembly.</param>
        /// <param name="responseJsonException">if set to <c>true</c> [response json exception].</param>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <returns>
        /// An instance of webserver.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer LoadApiControllers(
            this IWebServer webserver, 
            Assembly assembly = null,
            bool responseJsonException = false,
            string baseUrlPath = UrlPath.Root)
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            var webApiModule = webserver.Modules.OfType<WebApiModule>().FirstOrDefault();
            if (webApiModule == null)
            {
                webApiModule = new WebApiModule(baseUrlPath, responseJsonException);
                webserver = webserver.WithWebApi(responseJsonException: responseJsonException);
            }

            webApiModule.LoadApiControllers(assembly);
            return webserver;
        }

        /// <summary>
        /// Adds a <see cref="WebApiController"/> to a <see cref="WebApiModule"/>.
        /// </summary>
        /// <typeparam name="TWebApiController">The type of the web API controller.</typeparam>
        /// <param name="apiModule">The API module.</param>
        /// <returns><paramref name="apiModule"/> with the controller added.</returns>
        /// <exception cref="ArgumentNullException">apiModule</exception>
        public static WebApiModule WithApiController<TWebApiController>(this WebApiModule apiModule)
            where TWebApiController : WebApiController
        {
            if (apiModule == null)
                throw new ArgumentNullException(nameof(apiModule));

            apiModule.RegisterController<TWebApiController>();
            return apiModule;
        }

        /// <summary>
        /// Load all the WebApi Controllers in an assembly.
        /// </summary>
        /// <param name="apiModule">The Web API Module instance.</param>
        /// <param name="assembly">The assembly to load WebApi Controllers from. Leave null to load from the currently executing assembly.</param>
        /// <returns>The webserver instance.</returns>
        /// <exception cref="System.ArgumentNullException">webserver.</exception>
        public static WebApiModule LoadApiControllers(this WebApiModule apiModule, Assembly assembly = null)
        {
            if (apiModule == null)
                throw new ArgumentNullException(nameof(apiModule));

            var apiControllers = (assembly ?? Assembly.GetEntryAssembly()).GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition &&
                            x.IsSubclassOf(typeof(WebApiController)));

            foreach (var apiController in apiControllers)
            {
                $"Registering Web API controller '{apiController.Name}'".Debug(nameof(LoadApiControllers));
                apiModule.RegisterController(apiController);
            }

            return apiModule;
        }

        /// <summary>
        /// Register a <see cref="WebSocketsServer"/> in a <see cref="WebSocketsModule"/>.
        /// </summary>
        /// <typeparam name="TWebSocketsServer">The type of the web sockets server.</typeparam>
        /// <param name="webSocketsModule">The web sockets module.</param>
        /// <returns><paramref name="webSocketsModule"/> with the new server added.</returns>
        /// <exception cref="ArgumentNullException">webSocketsModule</exception>
        public static WebSocketsModule WithServer<TWebSocketsServer>(this WebSocketsModule webSocketsModule)
            where TWebSocketsServer : WebSocketsServer, new()
        {
            if (webSocketsModule == null)
                throw new ArgumentNullException(nameof(webSocketsModule));

            webSocketsModule.RegisterWebSocketsServer<TWebSocketsServer>();
            return webSocketsModule;
        }

        /// <summary>
        /// Load all the WebSockets in an assembly.
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebSocketsServer types from. Leave null to load from the currently executing assembly.</param>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <returns>
        /// An instance of webserver.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver</exception>
        /// <exception cref="System.ArgumentNullException">webserver.</exception>
        public static IWebServer LoadWebSockets(this IWebServer webserver, Assembly assembly = null, string baseUrlPath = UrlPath.Root)
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            var webSocketsModule = webserver.Modules.FirstOrDefault<WebSocketsModule>();
            if (webSocketsModule == null)
            {
                webSocketsModule = new WebSocketsModule(baseUrlPath);
                webserver.Modules.Add(webSocketsModule);
            }

            var socketServers = (assembly ?? Assembly.GetEntryAssembly()).GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition &&
                            x.IsSubclassOf(typeof(WebSocketsServer)));

            foreach (var socketServer in socketServers)
            {
                $"Registering WebSocket Server '{socketServer.Name}'".Debug(nameof(LoadWebSockets));
                webSocketsModule.RegisterWebSocketsServer(socketServer);
            }

            return webserver;
        }

        /// <summary>
        /// Add WebApi Controller to WebServer.
        /// </summary>
        /// <typeparam name="T">The type of Web API Controller.</typeparam>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="responseJsonException">if set to <c>true</c> [response json exception].</param>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <returns>
        /// An instance of webserver.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer WithWebApiController<T>(
            this IWebServer webserver, 
            bool responseJsonException = false,
            string baseUrlPath = UrlPath.Root)
            where T : WebApiController
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            var webApiModule = webserver.Modules.FirstOrDefault<WebApiModule>();

            if (webApiModule == null)
            {
                webApiModule = new WebApiModule(baseUrlPath, responseJsonException);
                webserver.Modules.Add(webApiModule);
            }

            webApiModule.RegisterController<T>();

            return webserver;
        }
    }
}
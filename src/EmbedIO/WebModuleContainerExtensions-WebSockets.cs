using System;
using System.Linq;
using System.Reflection;
using EmbedIO.Modules;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
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
        public static TContainer WithWebSocketsServersFromAssembly<TContainer>(this TContainer @this, string baseUrlPath, Assembly assembly = null)
            where TContainer : class, IWebModuleContainer
        {
            var webSocketsModule = @this.Modules.FirstOrDefault<WebSocketsModule>();
            if (webSocketsModule == null)
            {
                webSocketsModule = new WebSocketsModule(baseUrlPath);
                @this.Modules.Add(webSocketsModule);
            }

            var socketServers = (assembly ?? Assembly.GetEntryAssembly()).GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition &&
                            x.IsSubclassOf(typeof(WebSocketsServer)));

            foreach (var socketServer in socketServers)
            {
                $"Registering WebSocket Server '{socketServer.Name}'".Debug(nameof(WithWebSocketsServersFromAssembly));
                webSocketsModule.RegisterWebSocketsServer(socketServer);
            }

            return @this;
        }
    }
}
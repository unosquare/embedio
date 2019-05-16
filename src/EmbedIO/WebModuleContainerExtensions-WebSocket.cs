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
        /// Load all the WebSocket servers in an assembly.
        /// </summary>
        /// <param name="webserver">The webserver instance.</param>
        /// <param name="assembly">The assembly to load WebSocketServer types from. Leave null to load from the currently executing assembly.</param>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <returns>
        /// An instance of webserver.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver</exception>
        /// <exception cref="System.ArgumentNullException">webserver.</exception>
        public static TContainer WithWebSocketServersFromAssembly<TContainer>(this TContainer @this, string baseUrlPath, Assembly assembly = null)
            where TContainer : class, IWebModuleContainer
        {
            var module = @this.Modules.FirstOrDefault<WebSocketModule>();
            if (module == null)
            {
                module = new WebSocketModule(baseUrlPath);
                @this.Modules.Add(module);
            }

            var servers = (assembly ?? Assembly.GetEntryAssembly()).GetTypes()
                .Where(x => x.IsClass && !x.IsAbstract && !x.IsGenericTypeDefinition &&
                            x.IsSubclassOf(typeof(WebSocketServer)));

            foreach (var server in servers)
            {
                $"Registering WebSocket Server '{server.Name}'".Debug(nameof(WithWebSocketServersFromAssembly));
                module.RegisterWebSocketServer(server);
            }

            return @this;
        }
    }
}
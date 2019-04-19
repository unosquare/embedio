namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// A WebSockets module conforming to RFC 6455.
    /// </summary>
    public class WebSocketsModule : WebModuleBase, IDisposable
    {
        /// <summary>
        /// Holds the collection of paths and WebSockets Servers registered.
        /// </summary>
        private readonly Dictionary<string, WebSocketsServer> _serverMap =
            new Dictionary<string, WebSocketsServer>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsModule"/> class.
        /// </summary>
        public WebSocketsModule()
        {
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, async (context, ct) =>
            {
                if (!context.Request.IsWebSocketRequest)
                    return false;

                string path;

                // retrieve the request path
                switch (Server.RoutingStrategy)
                {
                    case RoutingStrategy.Wildcard:
                        path = context.RequestWilcardPath(_serverMap.Keys
                        .Where(k => k.Contains(ModuleMap.AnyPathRoute))
                        .Select(s => s.ToLowerInvariant()));
                        break;
                    case RoutingStrategy.Regex:
                        path = NormalizeRegexPath(context);
                        break;
                    default:
                        path = context.RequestPath();
                        break;
                }

                if (string.IsNullOrEmpty(path) || !_serverMap.ContainsKey(path))
                {
                    return false;
                }

                // Accept the WebSocket -- this is a blocking method until the WebSocketCloses
                await _serverMap[path].AcceptWebSocket(context, ct).ConfigureAwait(false);

                return true;
            });
        }

        /// <inheritdoc />
        public override string Name => nameof(WebSocketsModule);

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server.</typeparam>
        /// <exception cref="ArgumentException">Argument 'path' cannot be null;path.</exception>
        public void RegisterWebSocketsServer<T>()
            where T : WebSocketsServer, new()
        {
            RegisterWebSocketsServer(typeof(T));
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <param name="socketType">Type of the socket.</param>
        /// <exception cref="System.ArgumentNullException">socketType.</exception>
        /// <exception cref="System.ArgumentException">Argument 'socketType' needs a WebSocketHandlerAttribute - socketType.</exception>
        public void RegisterWebSocketsServer(Type socketType)
        {
            if (socketType == null)
                throw new ArgumentNullException(nameof(socketType));

            if (!(socketType.GetTypeInfo().GetCustomAttribute<WebSocketHandlerAttribute>()
                is WebSocketHandlerAttribute attribute))
            {
                throw new ArgumentException(
                    $"Argument '{nameof(socketType)}' needs a {nameof(WebSocketHandlerAttribute)}",
                    nameof(socketType));
            }

            _serverMap[attribute.Path] = (WebSocketsServer)Activator.CreateInstance(socketType);
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server.</typeparam>
        /// <param name="path">The path. For example: '/echo'.</param>
        /// <exception cref="ArgumentException">Argument 'path' cannot be null;path.</exception>
        public void RegisterWebSocketsServer<T>(string path)
            where T : WebSocketsServer, new()
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Argument 'path' cannot be null", nameof(path));

            _serverMap[path] = Activator.CreateInstance<T>();
        }

        /// <summary>
        /// Registers the web sockets server.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server.</typeparam>
        /// <param name="path">The path. For example: '/echo'.</param>
        /// <param name="server">The server.</param>
        /// <exception cref="System.ArgumentNullException">
        /// path
        /// or
        /// server.
        /// </exception>
        public void RegisterWebSocketsServer<T>(string path, T server)
            where T : WebSocketsServer
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            _serverMap[path] = server ?? throw new ArgumentNullException(nameof(server));
        }

        /// <inheritdoc />
        public override void RunWatchdog()
        {
            foreach (var instance in _serverMap)
                instance.Value.CancellationToken = CancellationToken;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var server in _serverMap.Select(y => y.Value).ToArray())
                server?.Dispose();
        }

        /// <summary>
        /// Normalizes a path meant for Regex matching returns the registered
        /// path in the internal map.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A string that represents the registered path in the internal map.</returns>
        private string NormalizeRegexPath(IHttpContext context)
        {
            var path = string.Empty;

            foreach (var route in _serverMap.Keys)
            {
                var urlParam = context.RequestRegexUrlParams(route);

                if (urlParam == null) continue;

                return route;
            }

            return path;
        }
    }
}

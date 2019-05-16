using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;

namespace EmbedIO.Modules
{

    /// <summary>
    /// A WebSocket module conforming to RFC 6455.
    /// </summary>
    public class WebSocketModule : WebModuleBase, IDisposable
    {
        /// <summary>
        /// Holds the collection of paths and WebSocket Servers registered.
        /// </summary>
        private readonly ConcurrentDictionary<string, WebSocketServer> _serverMap =
            new ConcurrentDictionary<string, WebSocketServer>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="routingStrategy">The routing strategy.</param>
        public WebSocketModule(string baseUrlPath, RoutingStrategy routingStrategy = RoutingStrategy.Regex)
            : base(baseUrlPath)
        {
            RoutingStrategy = routingStrategy;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WebSocketModule"/> class.
        /// </summary>
        ~WebSocketModule()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets the routing strategy.
        /// </summary>
        /// <value>
        /// The routing strategy.
        /// </value>
        public RoutingStrategy RoutingStrategy { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketServer Type.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server.</typeparam>
        /// <exception cref="ArgumentException">Validate 'path' cannot be null;path.</exception>
        public void RegisterWebSocketServer<T>()
            where T : WebSocketServer, new()
        {
            RegisterWebSocketServer(typeof(T));
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketServer Type.
        /// </summary>
        /// <param name="socketType">Type of the socket.</param>
        /// <exception cref="System.ArgumentNullException">socketType.</exception>
        /// <exception cref="System.ArgumentException">Validate 'socketType' needs a WebSocketHandlerAttribute - socketType.</exception>
        public void RegisterWebSocketServer(Type socketType)
        {
            if (socketType == null)
                throw new ArgumentNullException(nameof(socketType));

            if (!(socketType.GetCustomAttribute<WebSocketHandlerAttribute>()
                is WebSocketHandlerAttribute attribute))
            {
                throw new ArgumentException(
                    $"Validate '{nameof(socketType)}' needs a {nameof(WebSocketHandlerAttribute)}",
                    nameof(socketType));
            }

            _serverMap[attribute.Path] = (WebSocketServer) Activator.CreateInstance(socketType);
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketServer Type.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server.</typeparam>
        /// <param name="path">The path. For example: '/echo'.</param>
        /// <exception cref="ArgumentException">Validate 'path' cannot be null;path.</exception>
        public void RegisterWebSocketServer<T>(string path)
            where T : WebSocketServer, new()
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Validate 'path' cannot be null", nameof(path));

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
        public void RegisterWebSocketServer<T>(string path, T server)
            where T : WebSocketServer
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            _serverMap[path] = server ?? throw new ArgumentNullException(nameof(server));
        }

        /// <inheritdoc />
        public override void Start(CancellationToken ct)
        {
            foreach (var instance in _serverMap)
                instance.Value.CancellationToken = ct;
        }

        /// <inheritdoc />
        public override async Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct)
        {
            if (!context.Request.IsWebSocketRequest)
                return false;

            string finalPath;

            // retrieve the request path
            switch (RoutingStrategy)
            {
                case RoutingStrategy.Wildcard:
                    finalPath = context.RequestWilcardPath(_serverMap.Keys
                        .Where(k => k.Contains(ModuleMap.AnyPathRoute))
                        .Select(s => s.ToLowerInvariant()));
                    break;
                case RoutingStrategy.Regex:
                    finalPath = NormalizeRegexPath(context);
                    break;
                default:
                    finalPath = path;
                    break;
            }

            if (string.IsNullOrEmpty(finalPath) || !_serverMap.ContainsKey(finalPath))
            {
                return false;
            }

            // Accept the WebSocket -- this is a blocking method until the WebSocketCloses
            await _serverMap[finalPath].HandleWebSocket(context as IHttpContextImpl, ct).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

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
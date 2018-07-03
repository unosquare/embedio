namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Reflection;
    using Swan;
#if NET47
    using System.Net.WebSockets;
    using System.Text.RegularExpressions;
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// A WebSockets module conforming to RFC 6455
    /// Works only on Chrome 16+, Firefox 11+ and IE 10+
    /// This module is experimental and still needs extensive testing.
    /// </summary>
    public class WebSocketsModule : WebModuleBase
    {
        /// <summary>
        /// Holds the collection of paths and WebSockets Servers registered
        /// </summary>
        private readonly Dictionary<string, WebSocketsServer> _serverMap =
            new Dictionary<string, WebSocketsServer>(StringComparer.OrdinalIgnoreCase);

#if NETSTANDARD2_0
        private readonly Regex splitter = new Regex(@"(\s|[,;])+");
#endif
        
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Unosquare.Labs.EmbedIO.Modules.WebSocketsModule" /> class.
        /// </summary>
        public WebSocketsModule()
        {
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, async (context, ct) =>
            {
                if (context.Request.IsWebSocketRequest == false)
                    return false;

                string path;

                // retrieve the request path
                switch (Server.RoutingStrategy)
                {
                    case RoutingStrategy.Wildcard:
                        path = context.RequestWilcardPath(_serverMap.Keys
                        .Where(k => k.Contains("/" + ModuleMap.AnyPath))
                        .Select(s => s.ToLowerInvariant())
                        .ToArray());
                        break;
                    case RoutingStrategy.Regex:
                        path = NormalizeRegexPath(context);
                        break;
                    default:
                        path = context.RequestPath();
                        break;
                }

                if (string.IsNullOrEmpty(path) && !_serverMap.ContainsKey(path))
                {
                    return false;
                }

                // Accept the WebSocket -- this is a blocking method until the WebSocketCloses
                await _serverMap[path].AcceptWebSocket(context, ct);
                return true;
            });
        }

        /// <inheritdoc />
        public override string Name => nameof(WebSocketsModule).Humanize();

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server.</typeparam>
        /// <exception cref="ArgumentException">Argument 'path' cannot be null;path</exception>
        public void RegisterWebSocketsServer<T>()
            where T : WebSocketsServer, new()
        {
            RegisterWebSocketsServer(typeof(T));
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <param name="socketType">Type of the socket.</param>
        /// <exception cref="System.ArgumentNullException">socketType</exception>
        /// <exception cref="System.ArgumentException">Argument 'socketType' needs a WebSocketHandlerAttribute - socketType</exception>
        public void RegisterWebSocketsServer(Type socketType)
        {
            if (socketType == null)
                throw new ArgumentNullException(nameof(socketType));

            var attribute =
                socketType.GetTypeInfo().GetCustomAttributes(typeof(WebSocketHandlerAttribute), true).FirstOrDefault()
                    as
                    WebSocketHandlerAttribute;

            if (attribute == null)
            {
                throw new ArgumentException("Argument 'socketType' needs a WebSocketHandlerAttribute",
                    nameof(socketType));
            }

            _serverMap[attribute.Path] = (WebSocketsServer)Activator.CreateInstance(socketType);
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <typeparam name="T">The type of WebSocket server</typeparam>
        /// <param name="path">The path. For example: '/echo'</param>
        /// <exception cref="ArgumentException">Argument 'path' cannot be null;path</exception>
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
        /// <typeparam name="T">The type of WebSocket server</typeparam>
        /// <param name="path">The path. For example: '/echo'</param>
        /// <param name="server">The server.</param>
        /// <exception cref="System.ArgumentNullException">
        /// path
        /// or
        /// server
        /// </exception>
        public void RegisterWebSocketsServer<T>(string path, T server)
            where T : WebSocketsServer
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException(nameof(path));

            _serverMap[path] = server ?? throw new ArgumentNullException(nameof(server));
        }

        /// <summary>
        /// Normalizes a path meant for Regex matching returns the registered
        /// path in the internal map.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A string that represents the registered path in the internal map</returns>
        private string NormalizeRegexPath(HttpListenerContext context)
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

    /// <summary>
    /// A base class that defines how to handle WebSockets connections.
    /// It keeps a list of connected WebSockets and has the basic logic to handle connections
    /// and data transmission
    /// </summary>
    public abstract class WebSocketsServer : IDisposable
    {
        private readonly bool _enableDisconnectedSocketColletion;
        private readonly object _syncRoot = new object();
        private readonly List<WebSocketContext> _mWebSockets = new List<WebSocketContext>(10);
#if NET47
        private readonly int _maximumMessageSize;
#endif
        private bool _isDisposing;
        private CancellationToken _ct;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsServer" /> class.
        /// </summary>
        /// <param name="enableConnectionWatchdog">if set to <c>true</c> [enable connection watchdog].</param>
        /// <param name="maxMessageSize">Maximum size of the message in bytes. Enter 0 or negative number to prevent checks.</param>
        protected WebSocketsServer(bool enableConnectionWatchdog, int maxMessageSize = 0)
        {
            _enableDisconnectedSocketColletion = enableConnectionWatchdog;
#if NET47
            _maximumMessageSize = maxMessageSize;
#endif

            RunConnectionWatchdog();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsServer"/> class. With dead connection watchdog and no message size checks.
        /// </summary>
        protected WebSocketsServer()
            : this(true)
        {
            // placeholder
        }

        /// <summary>
        /// Gets the Currently-Connected WebSockets.
        /// </summary>
        /// <value>
        /// The web sockets.
        /// </value>
        public ReadOnlyCollection<WebSocketContext> WebSockets
        {
            get
            {
                lock (_syncRoot)
                {
                    return new ReadOnlyCollection<WebSocketContext>(_mWebSockets);
                }
            }
        }

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        /// <value>
        /// The name of the server.
        /// </value>
        public abstract string ServerName { get; }

        /// <summary>
        /// Gets the Encoding used to use the Send method to send a string. The default is UTF8 per the WebSocket specification.
        /// </summary>
        /// <value>
        /// The Encoding to be used.
        /// </value>
        protected System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;

        /// <summary>
        /// Accepts the WebSocket connection.
        /// This is a blocking call so it must be called within an independent thread.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous of websocket connection operation</returns>
#if NET47
        public async Task AcceptWebSocket(System.Net.HttpListenerContext context, CancellationToken ct)
#else
        public async Task AcceptWebSocket(HttpListenerContext context, CancellationToken ct)
#endif
        {
            _ct = ct;

            // first, accept the websocket
            $"{ServerName} - Accepting WebSocket . . .".Debug(nameof(WebSocketsServer));

#if NET47
            const int receiveBufferSize = 2048;
#endif

            var webSocketContext =
#if NET47
                await context.AcceptWebSocketAsync(
                    subProtocol: null,
                    receiveBufferSize: receiveBufferSize,
                    keepAliveInterval: TimeSpan.FromSeconds(30));
#else
                await context.AcceptWebSocketAsync();
#endif

            // remove the disconnected clients
            CollectDisconnected();
            lock (_syncRoot)
            {
                // add the newly-connected client
                _mWebSockets.Add(webSocketContext);
            }

            $"{ServerName} - WebSocket Accepted - There are {WebSockets.Count} sockets connected.".Debug(
                nameof(WebSocketsServer));

            // call the abstract member
#if NET47
            OnClientConnected(webSocketContext, context.Request.LocalEndPoint, context.Request.RemoteEndPoint);
#else
            OnClientConnected(webSocketContext);
#endif

            try
            {
#if NET47
                // define a receive buffer
                var receiveBuffer = new byte[receiveBufferSize];

                // define a dynamic buffer that holds multi-part receptions
                var receivedMessage = new List<byte>(receiveBuffer.Length * 2);

                // poll the WebSockets connections for reception
                while (webSocketContext.WebSocket.State == WebSocketState.Open)
                {
                    // retrieve the result (blocking)
                    var receiveResult =
                        await webSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), ct);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        // close the connection if requested by the client
                        await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, ct);
                        return;
                    }

                    var frameBytes = new byte[receiveResult.Count];
                    Array.Copy(receiveBuffer, frameBytes, frameBytes.Length);
                    this.OnFrameReceived(webSocketContext, frameBytes, receiveResult);

                    // add the response to the multi-part response
                    receivedMessage.AddRange(frameBytes);

                    if (receivedMessage.Count > _maximumMessageSize && _maximumMessageSize > 0)
                    {
                        // close the connection if message exceeds max length
                        await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig,
                            $"Message too big. Maximum is {_maximumMessageSize} bytes.",
                            ct);

                        // exit the loop; we're done
                        return;
                    }

                    // if we're at the end of the message, process the message
                    if (receiveResult.EndOfMessage)
                    {
                        this.OnMessageReceived(webSocketContext, receivedMessage.ToArray(), receiveResult);
                        receivedMessage.Clear();
                    }
                }
#else
                webSocketContext.WebSocket.OnMessage += (s, e) =>
                {
                    var isText = e.IsText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;

                    OnMessageReceived(webSocketContext,
                        e.RawData,
                        new WebSocketReceiveResult(e.RawData.Length, isText, e.Opcode == Opcode.Close));
                };

                while (webSocketContext.WebSocket.IsConnected)
                {
                    await Task.Delay(500, ct);
                }
#endif
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocketsServer));
            }
            finally
            {
                // once the loop is completed or connection aborted, remove the WebSocket
                RemoveWebSocket(webSocketContext);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposing) return;

            _isDisposing = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sends a UTF-8 payload
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        /// <param name="payload">The payload.</param>
        protected virtual async void Send(WebSocketContext webSocket, string payload)
        {
            try
            {
                if (payload == null) payload = string.Empty;
                var buffer = Encoding.GetBytes(payload);

#if NET47
                await webSocket.WebSocket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    _ct);
#else
                await webSocket.WebSocket.SendAsync(buffer, Opcode.Text, _ct);
#endif
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocketsServer));
            }
        }

        /// <summary>
        /// Sends a binary payload
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        /// <param name="payload">The payload.</param>
        protected virtual async void Send(WebSocketContext webSocket, byte[] payload)
        {
            try
            {
                if (payload == null) payload = new byte[0];

#if NET47
                await webSocket.WebSocket.SendAsync(
                    new ArraySegment<byte>(payload),
                    WebSocketMessageType.Binary,
                    true,
                    _ct);
#else
                await webSocket.WebSocket.SendAsync(payload, Opcode.Binary, _ct);
#endif
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocketsServer));
            }
        }

        /// <summary>
        /// Broadcasts the specified payload to all connected WebSockets clients.
        /// </summary>
        /// <param name="payload">The payload.</param>
        protected virtual void Broadcast(byte[] payload)
        {
            WebSockets.ToList()
                .ForEach(wsc => Send(wsc, payload));
        }

        /// <summary>
        /// Broadcasts the specified payload to all connected WebSockets clients.
        /// </summary>
        /// <param name="payload">The payload.</param>
        protected virtual void Broadcast(string payload)
        {
            WebSockets.ToList()
                .ForEach(wsc => Send(wsc, payload));
        }

        /// <summary>
        /// Closes the specified web socket, removes it and disposes it.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        protected virtual async void Close(WebSocketContext webSocket)
        {
            if (webSocket == null)
                return;

            try
            {
#if NET47
                await webSocket.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _ct);
#else
                await webSocket.WebSocket.CloseAsync(ct: _ct);
#endif
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocketsServer));
            }
            finally
            {
                RemoveWebSocket(webSocket);
            }
        }

        /// <summary>
        /// Called when this WebSockets Server receives a full message (EndOfMessage) form a WebSockets client.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="rxBuffer">The response buffer.</param>
        /// <param name="rxResult">The response result.</param>
        protected abstract void OnMessageReceived(
            WebSocketContext context,
            byte[] rxBuffer,
            WebSocketReceiveResult rxResult);

        /// <summary>
        /// Called when this WebSockets Server receives a message frame regardless if the frame represents the EndOfMessage.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="rxBuffer">The response buffer.</param>
        /// <param name="rxResult">The response result.</param>
        protected abstract void OnFrameReceived(
            WebSocketContext context,
            byte[] rxBuffer,
            WebSocketReceiveResult rxResult);

#if NET47
        /// <summary>
        /// Called when this WebSockets Server accepts a new WebSockets client.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="localEndPoint">The local endpoint.</param>
        /// <param name="remoteEndPoint">The remote endpoint.</param>
        protected abstract void OnClientConnected(
            WebSocketContext context,
            System.Net.IPEndPoint localEndPoint,
            System.Net.IPEndPoint remoteEndPoint);
#else
        /// <summary>
        /// Called when this WebSockets Server accepts a new WebSockets client.
        /// </summary>
        /// <param name="context">The context.</param>
        protected abstract void OnClientConnected(WebSocketContext context);
#endif

        /// <summary>
        /// Called when the server has removed a WebSockets connected client for any reason.
        /// </summary>
        /// <param name="context">The context.</param>
        protected abstract void OnClientDisconnected(WebSocketContext context);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeAll"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposeAll)
        {
            // We only have managed resources here.
            // if called with false, return.
            if (disposeAll == false) return;

            lock (_syncRoot)
            {
                _mWebSockets.ForEach(Close);
            }

            CollectDisconnected();
        }

        /// <summary>
        /// Runs the connection watchdog.
        /// Removes and disposes stale WebSockets connections every 10 minutes.
        /// </summary>
        private void RunConnectionWatchdog()
        {
            if (_enableDisconnectedSocketColletion == false) return;

            var watchDogTask = Task.Factory.StartNew(async () =>
            {
                while (_isDisposing == false)
                {
                    if (_isDisposing == false)
                        CollectDisconnected();

                    // TODO: make this sleep configurable.
                    await Task.Delay(TimeSpan.FromSeconds(30), _ct);
                }
            }, _ct);
        }

        /// <summary>
        /// Removes and disposes the web socket.
        /// </summary>
        /// <param name="webSocketContext">The web socket context.</param>
        private void RemoveWebSocket(WebSocketContext webSocketContext)
        {
            webSocketContext.WebSocket?.Dispose();

            lock (_syncRoot)
            {
                _mWebSockets.Remove(webSocketContext);
            }

            OnClientDisconnected(webSocketContext);
        }

        /// <summary>
        /// Removes and disposes all disconnected sockets
        /// </summary>
        private void CollectDisconnected()
        {
            var collectedCount = 0;
            lock (_syncRoot)
            {
                for (var i = _mWebSockets.Count - 1; i >= 0; i--)
                {
                    var currentSocket = _mWebSockets[i];

                    if (currentSocket.WebSocket == null || currentSocket.WebSocket.State == WebSocketState.Open)
                        continue;

                    RemoveWebSocket(currentSocket);
                    collectedCount++;
                }
            }

            $"{ServerName} - Collected {collectedCount} sockets. WebSocket Count: {WebSockets.Count}".Debug(
                nameof(WebSocketsServer));
        }
    }
}

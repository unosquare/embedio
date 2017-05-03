namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Reflection;
    using Swan;
#if NET46
    using System.Net.WebSockets;
#else
    using Net;
#endif

    /// <summary>
    /// A WebSockets module conforming to RFC 6455
    /// Works only on Chrome 16+, FireFox 11+ and IE 10+
    /// This module is experimental and still needs extensive testing.
    /// </summary>
    public class WebSocketsModule : WebModuleBase
    {
        /// <summary>
        /// Holds the collection of paths and WebSockets Servers registered
        /// </summary>
        private readonly Dictionary<string, WebSocketsServer> _serverMap =
            new Dictionary<string, WebSocketsServer>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initialize WebSocket module
        /// </summary>
        public WebSocketsModule()
        {
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, async (context, ct) =>
            {
                // check if it is a WebSocket request (this only works with Win8 and Windows 2012)
                if (context.Request.IsWebSocketRequest == false)
                    return false;

                // retrieve the request path
                var path = context.RequestPath();

                // match the request path
                if (!_serverMap.ContainsKey(path))
                    return false;

                // Accept the WebSocket -- this is a blocking method until the WebSocketCloses
                await _serverMap[path].AcceptWebSocket(context, ct);
                return true;
            });
        }

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => nameof(WebSocketsModule).Humanize();

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="ArgumentException">Argument 'path' cannot be null;path</exception>
        public void RegisterWebSocketsServer<T>()
            where T : WebSocketsServer, new()
        {
            RegisterWebSocketsServer(typeof(T));
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <param name="socketType"></param>
        /// <exception cref="ArgumentException">Argument 'socketType' cannot be null;socketType</exception>
        public void RegisterWebSocketsServer(Type socketType)
        {
            if (socketType == null)
                throw new ArgumentException("Argument 'socketType' cannot be null", nameof(socketType));

            var attribute =
                socketType.GetTypeInfo().GetCustomAttributes(typeof(WebSocketHandlerAttribute), true).FirstOrDefault()
                    as
                    WebSocketHandlerAttribute;

            if (attribute == null)
                throw new ArgumentException("Argument 'socketType' needs a WebSocketHandlerAttribute",
                    nameof(socketType));

            _serverMap[attribute.Path] = (WebSocketsServer)Activator.CreateInstance(socketType);
        }

        /// <summary>
        /// Registers the web sockets server given a WebSocketsServer Type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path. For example: '/echo'</param>
        /// <param name="server">The server.</param>
        /// <exception cref="ArgumentException">Argument 'server' cannot be null;server</exception>
        public void RegisterWebSocketsServer<T>(string path, T server)
            where T : WebSocketsServer
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Argument 'path' cannot be null", nameof(path));
            if (server == null)
                throw new ArgumentException("Argument 'server' cannot be null", nameof(server));

            _serverMap[path] = server;
        }
    }

    /// <summary>
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WebSocketHandlerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandlerAttribute"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentException">The argument 'paths' must be specified.</exception>
        public WebSocketHandlerAttribute(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The argument 'path' must be specified.");

            Path = path;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        public string Path { get; }
    }

    /// <summary>
    /// A base class that defines how to handle WebSockets connections.
    /// It keeps a list of connected WebSockets and has the basic logic to handle connections
    /// and data transmission
    /// </summary>
    public abstract class WebSocketsServer : IDisposable
    {
        private bool _isDisposing;
        private readonly bool _enableDisconnectedSocketColletion;
        private readonly object _syncRoot = new object();
        private readonly List<WebSocketContext> _mWebSockets = new List<WebSocketContext>(10);
        private CancellationToken _ct = default(CancellationToken);
#if NET46
        private readonly int _maximumMessageSize;
#endif

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
        /// Initializes a new instance of the <see cref="WebSocketsServer" /> class.
        /// </summary>
        /// <param name="enableConnectionWatchdog">if set to <c>true</c> [enable connection watchdog].</param>
        /// <param name="maxMessageSize">Maximum size of the message in bytes. Enter 0 or negative number to prevent checks.</param>
        protected WebSocketsServer(bool enableConnectionWatchdog, int maxMessageSize)
        {
            _enableDisconnectedSocketColletion = enableConnectionWatchdog;
#if NET46
            _maximumMessageSize = maxMessageSize;
#endif

            RunConnectionWatchdog();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsServer"/> class. With dead connection watchdog and no message size checks.
        /// </summary>
        protected WebSocketsServer()
            : this(true, 0)
        {
            // placeholder
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
                    await Task.Delay(30 * 1000, _ct);
                }
            }, _ct);
        }

        /// <summary>
        /// Accepts the WebSocket connection.
        /// This is a blocking call so it must be called within an independent thread.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns></returns>
#if NET46
        public async Task AcceptWebSocket(System.Net.HttpListenerContext context, CancellationToken ct)
#else
        public async Task AcceptWebSocket(HttpListenerContext context, CancellationToken ct)
#endif
        {
            _ct = ct;

            // first, accept the websocket
            $"{ServerName} - Accepting WebSocket . . .".Debug(nameof(WebSocketsServer));

#if NET46
            const int receiveBufferSize = 2048;
#endif

            var webSocketContext =
#if NET46
                await context.AcceptWebSocketAsync(subProtocol: null, receiveBufferSize: receiveBufferSize,
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
            
            $"{ServerName} - WebSocket Accepted - There are {WebSockets.Count} sockets connected.".Debug(nameof(WebSocketsServer));

            // call the abstract member
            OnClientConnected(webSocketContext);

            try
            {
#if NET46
// define a receive buffer
                var receiveBuffer = new byte[receiveBufferSize];
                // define a dynamic buffer that holds multi-part receptions
                var receivedMessage = new List<byte>(receiveBuffer.Length * 2);

                // poll the WebSockets connections for reception
                while (webSocketContext.WebSocket.State == WebSocketState.Open)
                {
                    // retrieve the result (blocking)
                    var receiveResult = await webSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), ct);

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
                // TODO: Pending OnFrameReceived
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
        /// <returns></returns>
        private void CollectDisconnected()
        {
            var collectedCount = 0;
            lock (_syncRoot)
            {
                for (var i = _mWebSockets.Count - 1; i >= 0; i--)
                {
                    var currentSocket = _mWebSockets[i];

                    if (currentSocket.WebSocket != null &&
                        currentSocket.WebSocket.State != WebSocketState.Open)
                    {
                        RemoveWebSocket(currentSocket);
                        collectedCount++;
                    }
                }
            }
            
            $"{ServerName} - Collected {collectedCount} sockets. WebSocket Count: {WebSockets.Count}".Debug(nameof(WebSocketsServer));
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
                var buffer = System.Text.Encoding.UTF8.GetBytes(payload);

#if NET46
                await webSocket.WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
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

#if NET46
                await webSocket.WebSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true,
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
            var sockets = WebSockets.ToArray();
            foreach (var wsc in sockets)
                Send(wsc, payload);
        }

        /// <summary>
        /// Broadcasts the specified payload to all connected WebSockets clients.
        /// </summary>
        /// <param name="payload">The payload.</param>
        protected virtual void Broadcast(string payload)
        {
            var sockets = WebSockets.ToArray();
            foreach (var wsc in sockets)
                Send(wsc, payload);
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
#if NET46
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
        protected abstract void OnMessageReceived(WebSocketContext context, byte[] rxBuffer,
            WebSocketReceiveResult rxResult);

        /// <summary>
        /// Called when this WebSockets Server receives a message frame regardless if the frame represents the EndOfMessage.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="rxBuffer">The response buffer.</param>
        /// <param name="rxResult">The response result.</param>
        protected abstract void OnFrameReceived(WebSocketContext context, byte[] rxBuffer,
            WebSocketReceiveResult rxResult);

        /// <summary>
        /// Called when this WebSockets Server accepts a new WebSockets client.
        /// </summary>
        /// <param name="context">The context.</param>
        protected abstract void OnClientConnected(WebSocketContext context);

        /// <summary>
        /// Called when the server has removed a WebSockets connected client for any reason.
        /// </summary>
        /// <param name="context">The context.</param>
        protected abstract void OnClientDisconnected(WebSocketContext context);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposing) return;

            _isDisposing = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposeAll"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposeAll)
        {
            // We only have managed resources here.
            // if called with false, return.
            if (disposeAll == false) return;

            foreach (var webSocket in _mWebSockets)
            {
                Close(webSocket);
            }

            CollectDisconnected();
        }

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        /// <value>
        /// The name of the server.
        /// </value>
        public abstract string ServerName { get; }
    }
}
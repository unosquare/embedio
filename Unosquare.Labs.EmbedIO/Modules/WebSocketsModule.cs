namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Net.WebSockets;
    using System.Threading;

    /// <summary>
    /// A WebSockets module confirming to RFC 6455
    /// Works only on Chrome 16+, FireFox 11+ and IE 10+
    /// This module is experimental and still needs extensive testing.
    /// </summary>
    public class WebSocketsModule : WebModuleBase
    {
        /// <summary>
        /// Holds the collection of paths and WebSockets Servers registered
        /// </summary>
        private readonly Dictionary<string, WebSocketsServer> _serverMap =
            new Dictionary<string, WebSocketsServer>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Initialize WebSocket module
        /// </summary>
        public WebSocketsModule()
            : base()
        {
            this.AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (server, context) =>
            {

                // check if it is a WebSocket request (this only works with Win8 and Windows 2012)
                if (context.Request.IsWebSocketRequest == false)
                    return false;

                // retrieve the request path
                var path = context.RequestPath();

                // match the request path
                if (_serverMap.ContainsKey(path))
                {
                    // Accept the WebSocket -- this is a blocking method until the WebSocketCloses
                    _serverMap[path].AcceptWebSocket(server, context);
                    return true;
                }

                return false;

            });
        }

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name
        {
            get { return "WebSockets Module"; }
        }

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
                throw new ArgumentException("Argument 'socketType' cannot be null", "socketType");

            var attribute =
                socketType.GetCustomAttributes(typeof(WebSocketHandlerAttribute), true).FirstOrDefault() as
                    WebSocketHandlerAttribute;

            if (attribute == null)
                throw new ArgumentException("Argument 'socketType' needs a WebSocketHandlerAttribute", "socketType");

            this._serverMap[attribute.Path] = (WebSocketsServer)Activator.CreateInstance(socketType);
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
                throw new ArgumentException("Argument 'path' cannot be null", "path");

            this._serverMap[path] = Activator.CreateInstance<T>();
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
                throw new ArgumentException("Argument 'path' cannot be null", "path");
            if (server == null)
                throw new ArgumentException("Argument 'server' cannot be null", "server");

            this._serverMap[path] = server;
        }
    }

    /// <summary>
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class WebSocketHandlerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandlerAttribute"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentException">The argument 'paths' must be specified.</exception>
        public WebSocketHandlerAttribute(string path)
        {
            if (path == null || string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The argument 'path' must be specified.");

            this.Path = path;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        public string Path { get; private set; }
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
        private readonly int _maximumMessageSize;
        private readonly object _syncRoot = new object();
        private readonly List<WebSocketContext> _mWebSockets = new List<WebSocketContext>(10);

        /// <summary>
        /// WebServer internal instance
        /// </summary>
        public WebServer WebServer { get; protected set; }

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
            this._enableDisconnectedSocketColletion = enableConnectionWatchdog;
            this._maximumMessageSize = maxMessageSize;
            
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
            var t = new Thread(() =>
            {
                while (_isDisposing == false)
                {
                    if (_isDisposing == false)
                        CollectDisconnected();

                    // TODO: make this sleep configurable.
                    Thread.Sleep(30 * 1000);
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };

            if (_enableDisconnectedSocketColletion)
                t.Start();
        }

        /// <summary>
        /// Accepts the WebSocket connection.
        /// This is a blocking call so it must be called within an independent thread.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        public void AcceptWebSocket(WebServer server, HttpListenerContext context)
        {
            // first, accept the websocket
            this.WebServer = server;
            server.Log.DebugFormat("{0} - Accepting WebSocket . . .", this.ServerName);
            const int receiveBufferSize = 2048;
            var webSocketContext =
                context.AcceptWebSocketAsync(subProtocol: null, receiveBufferSize: receiveBufferSize,
                    keepAliveInterval: TimeSpan.FromSeconds(30))
                    .GetAwaiter()
                    .GetResult();

            // remove the disconnected clients
            this.CollectDisconnected();
            lock (_syncRoot)
            {
                // add the newly-connected client
                _mWebSockets.Add(webSocketContext);
            }

            server.Log.DebugFormat("{0} - WebSocket Accepted - There are " + WebSockets.Count + " sockets connected.",
                this.ServerName);
            // call the abstract member
            this.OnClientConnected(webSocketContext);

            try
            {
                // define a receive buffer
                var receiveBuffer = new byte[receiveBufferSize];
                // define a dynamic buffer that holds multi-part receptions
                var receivedMessage = new List<byte>(receiveBuffer.Length * 2);

                // poll the WebSockets connections for reception
                while (webSocketContext.WebSocket.State == WebSocketState.Open)
                {
                    // retrieve the result (blocking)
                    var receiveResult =
                        webSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer),
                            CancellationToken.None).GetAwaiter().GetResult();
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        // close the connection if requested by the client
                        webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                            CancellationToken.None).GetAwaiter().GetResult();
                        return;
                    }

                    var frameBytes = new byte[receiveResult.Count];
                    Array.Copy(receiveBuffer, frameBytes, frameBytes.Length);
                    this.OnFrameReceived(webSocketContext, frameBytes, receiveResult);

                    // add the response to the multi-part response
                    receivedMessage.AddRange(frameBytes);

                    if (receivedMessage.Count > _maximumMessageSize && _maximumMessageSize > 0)
                    {
                        // close the connection if message excceeds max length
                        webSocketContext.WebSocket.CloseAsync(
                            WebSocketCloseStatus.MessageTooBig,
                            string.Format("Message too big. Maximum is {0} bytes.", _maximumMessageSize),
                            CancellationToken.None).GetAwaiter().GetResult();

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
            }
            catch (Exception ex)
            {
                server.Log.ErrorFormat("{0} - Error: {1}", this.ServerName, ex);
            }
            finally
            {
                // once the loop is completed or connection aborted, remove the WebSocket
                this.RemoveWebSocket(webSocketContext);
            }
        }

        /// <summary>
        /// Removes and disposes the web socket.
        /// </summary>
        /// <param name="webSocketContext">The web socket context.</param>
        private void RemoveWebSocket(WebSocketContext webSocketContext)
        {
            if (webSocketContext.WebSocket != null)
                webSocketContext.WebSocket.Dispose();

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
        private int CollectDisconnected()
        {
            var collectedCount = 0;
            lock (_syncRoot)
            {
                for (var i = this._mWebSockets.Count - 1; i >= 0; i--)
                {
                    var currentSocket = this._mWebSockets[i];
                    if (currentSocket.WebSocket != null && currentSocket.WebSocket.State != WebSocketState.Open)
                    {
                        RemoveWebSocket(currentSocket);
                        collectedCount++;
                    }

                }
            }

            if (this.WebServer != null)
                this.WebServer.Log.DebugFormat("{0} - Collected {1} sockets. WebSocket Count: {2}", this.ServerName,
                    collectedCount, this.WebSockets.Count);

            return collectedCount;
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
                await
                    webSocket.WebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                        CancellationToken.None);
            }
            catch (Exception ex)
            {
                WebServer.Log.Error(ex);
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
                await
                    webSocket.WebSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true,
                        CancellationToken.None);
            }
            catch (Exception ex)
            {
                WebServer.Log.Error(ex);
            }
        }

        /// <summary>
        /// Broadcasts the specified payload to all connected WebSockets clients.
        /// </summary>
        /// <param name="payload">The payload.</param>
        protected virtual void Broadcast(byte[] payload)
        {
            var sockets = this.WebSockets.ToArray();
            foreach (var wsc in sockets)
                this.Send(wsc, payload);
        }

        /// <summary>
        /// Broadcasts the specified payload to all connected WebSockets clients.
        /// </summary>
        /// <param name="payload">The payload.</param>
        protected virtual void Broadcast(string payload)
        {
            var sockets = this.WebSockets.ToArray();
            foreach (var wsc in sockets)
                this.Send(wsc, payload);
        }

        /// <summary>
        /// Closes the specified web socket, removes it and disposes it.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        protected virtual async void Close(WebSocketContext webSocket)
        {
            if (webSocket == null) return;

            try
            {
                await
                    webSocket.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                        CancellationToken.None);
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
        /// <param name="rxBuffer">The rx buffer.</param>
        /// <param name="rxResult">The rx result.</param>
        protected abstract void OnMessageReceived(WebSocketContext context, byte[] rxBuffer,
            WebSocketReceiveResult rxResult);

        /// <summary>
        /// Called when this WebSockets Server receives a message frame regardless if the frame represents the EndOfMessage.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="rxBuffer">The rx buffer.</param>
        /// <param name="rxResult">The rx result.</param>
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
            if (this._isDisposing == false)
            {
                this._isDisposing = true;
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
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

            foreach (var webSocket in this._mWebSockets)
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
namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Swan;

    /// <inheritdoc />
    /// <summary>
    /// A base class that defines how to handle WebSockets connections.
    /// It keeps a list of connected WebSockets and has the basic logic to handle connections
    /// and data transmission.
    /// </summary>
    public abstract class WebSocketsServer : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly List<IWebSocketContext> _mWebSockets = new List<IWebSocketContext>(10);
#if NET47
        private readonly int _maximumMessageSize;
#endif
        private bool _isDisposing;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketsServer" /> class.
        /// </summary>
        /// <param name="enableConnectionWatchdog">if set to <c>true</c> [enable connection watchdog].</param>
        /// <param name="maxMessageSize">Maximum size of the message in bytes. Enter 0 or negative number to prevent checks.</param>
        protected WebSocketsServer(bool enableConnectionWatchdog, int maxMessageSize = 0)
        {
#if NET47
            _maximumMessageSize = maxMessageSize;
#endif
            if (enableConnectionWatchdog)
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
        public ReadOnlyCollection<IWebSocketContext> WebSockets
        {
            get
            {
                lock (_syncRoot)
                {
                    return new ReadOnlyCollection<IWebSocketContext>(_mWebSockets);
                }
            }
        }

        /// <summary>
        /// Gets or sets the cancellation token.
        /// </summary>
        /// <value>
        /// The cancellation token.
        /// </value>
        public CancellationToken CancellationToken { get; set; }

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
        /// <returns>A task that represents the asynchronous of websocket connection operation.</returns>
        public async Task AcceptWebSocket(IHttpContext context, CancellationToken ct)
        {
            const int receiveBufferSize = 2048;

            // first, accept the websocket
            $"{ServerName} - Accepting WebSocket . . .".Debug(nameof(WebSocketsServer));

            var webSocketContext = await context.AcceptWebSocketAsync(receiveBufferSize);

            // remove the disconnected clients
            CollectDisconnected();

            lock (_syncRoot)
            {
                // add the newly-connected client
                _mWebSockets.Add(webSocketContext);
            }

            $"{ServerName} - WebSocket Accepted - There are {WebSockets.Count} sockets connected.".Debug(
                nameof(WebSocketsServer));

            OnClientConnected(webSocketContext, context.Request.LocalEndPoint, context.Request.RemoteEndPoint);

            try
            {
#if NET47
                // define a receive buffer
                var receiveBuffer = new byte[receiveBufferSize];

                // define a dynamic buffer that holds multi-part receptions
                var receivedMessage = new List<byte>(receiveBuffer.Length * 2);

                // poll the WebSockets connections for reception
                while (webSocketContext.WebSocket.State == Net.WebSocketState.Open)
                {
                    // retrieve the result (blocking)
                    var receiveResult = new WebSocketReceiveResult(await ((System.Net.WebSockets.WebSocket) webSocketContext.WebSocket).ReceiveAsync(new ArraySegment<byte>(receiveBuffer), ct));

                    if (receiveResult.MessageType == (int) System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        // close the connection if requested by the client
                        await webSocketContext.WebSocket.CloseAsync(true, ct);
                        return;
                    }

                    var frameBytes = new byte[receiveResult.Count];
                    Array.Copy(receiveBuffer, frameBytes, frameBytes.Length);
                    OnFrameReceived(webSocketContext, frameBytes, receiveResult);

                    // add the response to the multi-part response
                    receivedMessage.AddRange(frameBytes);

                    if (receivedMessage.Count > _maximumMessageSize && _maximumMessageSize > 0)
                    {
                        // close the connection if message exceeds max length
                        await webSocketContext.WebSocket.CloseAsync(false, ct);

                        // exit the loop; we're done
                        return;
                    }

                    // if we're at the end of the message, process the message
                    if (receiveResult.EndOfMessage)
                    {
                        OnMessageReceived(webSocketContext, receivedMessage.ToArray(), receiveResult);
                        receivedMessage.Clear();
                    }
                }
#else
                ((Net.WebSocket) webSocketContext.WebSocket).OnMessage += async (s, e) =>
                {
                    if (e.Opcode == Net.Opcode.Close)
                    {
                        await webSocketContext.WebSocket.CloseAsync(true, ct: CancellationToken);
                        return;
                    }

                    OnMessageReceived(webSocketContext,
                        e.RawData,
                        new Net.WebSocketReceiveResult(e.RawData.Length, e.Opcode));
                };

                while (webSocketContext.WebSocket.State == Net.WebSocketState.Open || webSocketContext.WebSocket.State == Net.WebSocketState.Closing)
                {
                    await Task.Delay(500, ct);
                }
#endif
            }
            catch (TaskCanceledException)
            {
                // ignore
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
        /// Sends a UTF-8 payload.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        /// <param name="payload">The payload.</param>
        protected virtual async void Send(IWebSocketContext webSocket, string payload)
        {
            try
            {
                if (payload == null) payload = string.Empty;
                var buffer = Encoding.GetBytes(payload);
                
                await webSocket.WebSocket.SendAsync(buffer, true, CancellationToken);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebSocketsServer));
            }
        }

        /// <summary>
        /// Sends a binary payload.
        /// </summary>
        /// <param name="webSocket">The web socket.</param>
        /// <param name="payload">The payload.</param>
        protected virtual async void Send(IWebSocketContext webSocket, byte[] payload)
        {
            try
            {
                if (payload == null) payload = new byte[0];

                await webSocket.WebSocket.SendAsync(payload, false, CancellationToken);
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
        protected virtual async void Close(IWebSocketContext webSocket)
        {
            if (webSocket == null)
                return;

            try
            {
                await webSocket.WebSocket.CloseAsync(true, CancellationToken);
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
        /// <param name="buffer">The buffer.</param>
        /// <param name="result">The result.</param>
        protected abstract void OnMessageReceived(
            IWebSocketContext context,
            byte[] buffer,
            IWebSocketReceiveResult result);

        /// <summary>
        /// Called when this WebSockets Server receives a message frame regardless if the frame represents the EndOfMessage.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="result">The result.</param>
        protected abstract void OnFrameReceived(
            IWebSocketContext context,
            byte[] buffer,
            IWebSocketReceiveResult result);

        /// <summary>
        /// Called when this WebSockets Server accepts a new WebSockets client.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="localEndPoint">The local endpoint.</param>
        /// <param name="remoteEndPoint">The remote endpoint.</param>
        protected abstract void OnClientConnected(
            IWebSocketContext context,
            System.Net.IPEndPoint localEndPoint,
            System.Net.IPEndPoint remoteEndPoint);

        /// <summary>
        /// Called when the server has removed a WebSockets connected client for any reason.
        /// </summary>
        /// <param name="context">The context.</param>
        protected abstract void OnClientDisconnected(IWebSocketContext context);

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
            var watchDogTask = Task.Run(async () =>
            {
                while (_isDisposing == false)
                {
                    if (_isDisposing == false)
                        CollectDisconnected();

                    // TODO: make this sleep configurable.
                    await Task.Delay(TimeSpan.FromSeconds(30), CancellationToken);
                }
            }, CancellationToken);
        }

        /// <summary>
        /// Removes and disposes the web socket.
        /// </summary>
        /// <param name="webSocketContext">The web socket context.</param>
        private void RemoveWebSocket(IWebSocketContext webSocketContext)
        {
            webSocketContext.WebSocket?.Dispose();

            lock (_syncRoot)
            {
                _mWebSockets.Remove(webSocketContext);
            }

            OnClientDisconnected(webSocketContext);
        }

        /// <summary>
        /// Removes and disposes all disconnected sockets.
        /// </summary>
        private void CollectDisconnected()
        {
            var collectedCount = 0;
            lock (_syncRoot)
            {
                for (var i = _mWebSockets.Count - 1; i >= 0; i--)
                {
                    var currentSocket = _mWebSockets[i];

                    if (currentSocket.WebSocket == null || currentSocket.WebSocket.State == Net.WebSocketState.Open)
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
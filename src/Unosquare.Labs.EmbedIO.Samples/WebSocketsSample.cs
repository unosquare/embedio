namespace Unosquare.Labs.EmbedIO.Samples
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Modules;
    using Swan;

    public static class WebSocketsSample
    {
        /// <summary>
        /// Setups the specified server.
        /// </summary>
        /// <param name="server">The server.</param>
        public static void Setup(WebServer server)
        {
            server.RegisterModule(new WebSocketsModule());
            server.Module<WebSocketsModule>().RegisterWebSocketsServer<WebSocketsChatServer>();
            server.Module<WebSocketsModule>().RegisterWebSocketsServer<WebSocketsTerminalServer>();
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Defines a very simple chat server
    /// </summary>
    [WebSocketHandler("/chat")]
    public class WebSocketsChatServer : WebSocketsServer
    {
        public WebSocketsChatServer()
            : base(true)
        {
            // placeholder
        }
        
        /// <inheritdoc />
        protected override void OnMessageReceived(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            foreach (var ws in WebSockets.Where(ws => ws != context))
            {
                Send(ws, rxBuffer.ToText());
            }
        }

        
        /// <inheritdoc />
        public override string ServerName => nameof(WebSocketsChatServer);

        /// <inheritdoc />
        protected override void OnClientConnected(
            IWebSocketContext context, 
            System.Net.IPEndPoint localEndPoint,
            System.Net.IPEndPoint remoteEndPoint)
        {
            Send(context, "Welcome to the chat room!");

            foreach (var ws in WebSockets.Where(ws => ws != context))
            {
                Send(ws, "Someone joined the chat room.");
            }
        }
        
        /// <inheritdoc />
        protected override void OnFrameReceived(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            // placeholder
        }
        
        /// <inheritdoc />
        protected override void OnClientDisconnected(IWebSocketContext context)
        {
            Broadcast("Someone left the chat room.");
        }
    }
    
    /// <inheritdoc />
    /// <summary>
    /// Define a command-line interface terminal
    /// </summary>
    [WebSocketHandler("/terminal")]
    public class WebSocketsTerminalServer : WebSocketsServer
    {
        // we'll keep track of the processes here
        private readonly Dictionary<IWebSocketContext, Process> _processes = new Dictionary<IWebSocketContext, Process>();

        // The SyncRoot is used to send 1 thing at a time and multi-threaded Processes dictionary.
        private readonly object _syncRoot = new object();
        
        /// <inheritdoc />
        protected override void OnMessageReceived(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            lock (_syncRoot)
            {
                var arg = rxBuffer.ToText();
                _processes[context].StandardInput.WriteLine(arg);
            }
        }
        
        /// <inheritdoc />
        protected override void OnFrameReceived(IWebSocketContext context, byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            // don't process partial frames
        }

        /// <summary>
        /// Finds the context given the process.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns></returns>
        private IWebSocketContext FindContext(Process p)
        {
            lock (_syncRoot)
            {
                foreach (var kvp in _processes.Where(kvp => kvp.Value == p))
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        /// <inheritdoc />
        protected override void OnClientConnected(
            IWebSocketContext context, 
            System.Net.IPEndPoint localEndPoint,
            System.Net.IPEndPoint remoteEndPoint)
        {
            var process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    ErrorDialog = false,
                    FileName = "cmd.exe",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = "c:\\"
                }
            };

            process.OutputDataReceived += (s, e) =>
            {
                lock (_syncRoot)
                {
                    if ((s as Process).HasExited) return;
                    var ws = FindContext(s as Process);

                    if (ws != null && ws.WebSocket.State == Net.WebSocketState.Open)
                        Send(ws, e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                lock (_syncRoot)
                {
                    if ((s as Process).HasExited) return;
                    var ws = FindContext(s as Process);
                    if (ws != null && ws.WebSocket.State == Net.WebSocketState.Open)
                        Send(ws, e.Data);
                }
            };

            process.Exited += (s, e) =>
            {
                lock (_syncRoot)
                {
                    var ws = FindContext(s as Process);
                    if (ws != null && ws.WebSocket.State == Net.WebSocketState.Open)
                        ws.WebSocket.CloseAsync().GetAwaiter().GetResult();
                }
            };

            // add the process to the context
            lock (_syncRoot)
            {
                _processes[context] = process;
            }

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

        }
        
        /// <inheritdoc />
        protected override void OnClientDisconnected(IWebSocketContext context)
        {
            lock (_syncRoot)
            {
                if (_processes[context].HasExited == false)
                    _processes[context].Kill();
            }
        }
        
        /// <inheritdoc />
        public override string ServerName => nameof(WebSocketsTerminalServer);
    }
}
namespace Unosquare.Labs.EmbedIO.Command
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.WebSockets;
    using Unosquare.Labs.EmbedIO.Modules;
    using Unosquare.Swan;

    public static class WebSocketWatcher
    {
        public static void Setup()
        {            
            using (var server = new WebServer("http://localhost:9697/"))
            {
                server.RegisterModule(new WebSocketsModule());
                server.Module<WebSocketsModule>().RegisterWebSocketsServer<WebSocketsWatcherServer>();

                server.RunAsync();

                Console.ReadKey(true);
            }
        }
    }

    [WebSocketHandler("/watcher")]
    public class WebSocketsWatcherServer : WebSocketsServer
    {
        public WebSocketsWatcherServer()
            : base(true)
        {

        }

        public override string ServerName => nameof(WebSocketWatcher);

        protected override void OnClientConnected(WebSocketContext context, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Send(context, "Watching Files!!!");

            foreach (var ws in WebSockets.Where(ws => ws != context))
            {
                Send(ws, "Refresh!!!");
            }
        }

        protected override void OnClientDisconnected(WebSocketContext context)
        {
            Send(context, "Close connection!!!");
        }

        protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            // placeholder
        }

        protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            foreach (var ws in WebSockets.Where(ws => ws != context))
            {
                Send(ws, rxBuffer.ToText());
            }
        }
    }

}

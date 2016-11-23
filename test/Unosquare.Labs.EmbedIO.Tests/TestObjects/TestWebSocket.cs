using System;
using System.Text;
#if NET46
using System.Net.WebSockets;
#else
using Unosquare.Net;
#endif
using Unosquare.Labs.EmbedIO.Modules;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    [WebSocketHandler("/test")]
    public class TestWebSocket : WebSocketsServer
    {
        protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            Send(context, "HELLO");
        }

        protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            WebServer.Log.DebugFormat("Data frame: {0}", Encoding.UTF8.GetString(rxBuffer));
        }

        protected override void OnClientConnected(WebSocketContext context)
        {
            Send(context, "WELCOME");
        }

        protected override void OnClientDisconnected(WebSocketContext context)
        {
            Send(context, "ADIOS");
        }

        public override string ServerName => "TestWebSocket";
    }
}
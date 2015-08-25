using System;
using System.Net.WebSockets;
using Unosquare.Labs.EmbedIO.Modules;

namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    [WebSocketHandler("/test")]
    public class TestWebSocket : WebSocketsServer
    {
        protected override void OnMessageReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            this.Send(context, "HELLO");
        }

        protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            throw new NotImplementedException();
        }

        protected override void OnClientConnected(WebSocketContext context)
        {
            this.Send(context, "WELCOME");
        }

        protected override void OnClientDisconnected(WebSocketContext context)
        {
            this.Send(context, "ADIOS");
        }

        public override string ServerName
        {
            get { return "TestWebSocket"; }
        }
    }
}

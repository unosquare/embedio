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
            throw new NotImplementedException();
        }

        protected override void OnFrameReceived(WebSocketContext context, byte[] rxBuffer, WebSocketReceiveResult rxResult)
        {
            throw new NotImplementedException();
        }

        protected override void OnClientConnected(WebSocketContext context)
        {
            throw new NotImplementedException();
        }

        protected override void OnClientDisconnected(WebSocketContext context)
        {
            throw new NotImplementedException();
        }

        public override string ServerName
        {
            get { return "TestWebSocket"; }
        }
    }
}

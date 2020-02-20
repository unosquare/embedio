using System.Threading.Tasks;
using EmbedIO.WebSockets;

namespace EmbedIO.Samples
{
    /// <summary>
    /// Defines a very simple chat server.
    /// </summary>
    public class WebSocketChatModule : WebSocketModule
    {
        public WebSocketChatModule(string urlPath)
            : base(urlPath, true)
        {
        }

        /// <inheritdoc />
        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] buffer,
            IWebSocketReceiveResult result)
            => SendToOthersAsync(context, Encoding.GetString(buffer));

        /// <inheritdoc />
        protected override Task OnClientConnectedAsync(IWebSocketContext context)
            => Task.WhenAll(
                SendAsync(context, "Welcome to the chat room!"),
                SendToOthersAsync(context, "Someone joined the chat room."));

        /// <inheritdoc />
        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
            => SendToOthersAsync(context, "Someone left the chat room.");

        private Task SendToOthersAsync(IWebSocketContext context, string payload)
            => BroadcastAsync(payload, c => c != context);
    }
}
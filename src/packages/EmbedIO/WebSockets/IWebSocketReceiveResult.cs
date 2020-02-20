namespace EmbedIO.WebSockets
{
    /// <summary>
    /// Interface for WebSocket Receive Result object.
    /// </summary>
    public interface IWebSocketReceiveResult
    {
        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        int Count { get; }

        /// <summary>
        /// Gets a value indicating whether [end of message].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [end of message]; otherwise, <c>false</c>.
        /// </value>
        bool EndOfMessage { get; }

        /// <summary>
        /// Gets the type of the message.
        /// </summary>
        /// <value>
        /// The type of the message.
        /// </value>
        int MessageType { get; }
    }
}

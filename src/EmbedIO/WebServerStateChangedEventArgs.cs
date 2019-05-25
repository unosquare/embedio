using System;

namespace EmbedIO
{
    /// <summary>
    /// Represents event arguments whenever the state of a web server changes.
    /// </summary>
    public class WebServerStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldState">The old state.</param>
        /// <param name="newState">The new state.</param>
        public WebServerStateChangedEventArgs(WebServerState oldState, WebServerState newState)
        {
            OldState = oldState;
            NewState = newState;
        }
    
        /// <summary>
        /// Gets the state to which the application service changed.
        /// </summary>
        public WebServerState NewState { get; }

        /// <summary>
        /// Gets the old state.
        /// </summary>
        public WebServerState OldState { get; }
    }
}
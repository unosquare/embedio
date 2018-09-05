namespace Unosquare.Net
{
    using System;

    /// <summary>
    /// Represents the event data for the <see cref="WebSocket.OnClose"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   That event occurs when the WebSocket connection has been closed.
    ///   </para>
    ///   <para>
    ///   If you would like to get the reason for the close, you should access
    ///   the <see cref="Code"/> or <see cref="Reason"/> property.
    ///   </para>
    /// </remarks>
    public class CloseEventArgs : EventArgs
    {
        internal CloseEventArgs(PayloadData payloadData = null)
        {
            PayloadData = payloadData ?? new PayloadData();
        }

        internal CloseEventArgs(CloseStatusCode code, string reason = null)
        {
            PayloadData = new PayloadData((ushort) code, reason);
        }

        /// <summary>
        /// Gets the status code for the close.
        /// </summary>
        /// <value>
        /// A <see cref="ushort"/> that represents the status code for the close if any.
        /// </value>
        public ushort Code => PayloadData.Code;

        /// <summary>
        /// Gets the reason for the close.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the reason for the close if any.
        /// </value>
        public string Reason => PayloadData.Reason ?? string.Empty;

        /// <summary>
        /// Gets a value indicating whether the connection has been closed cleanly.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection has been closed cleanly; otherwise, <c>false</c>.
        /// </value>
        public bool WasClean { get; internal set; }

        internal PayloadData PayloadData { get; }
    }

    /// <summary>
    /// The event arguments for connection failure events.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ConnectionFailureEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionFailureEventArgs"/> class.
        /// </summary>
        /// <param name="ex">The ex.</param>
        public ConnectionFailureEventArgs(Exception ex)
        {
            Error = ex;
        }

        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get; }
    }
}
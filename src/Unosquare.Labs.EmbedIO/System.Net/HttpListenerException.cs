namespace Unosquare.Net
{
    using System;

    /// <summary>
    /// Represents an HTTP Listener's exception.
    /// </summary>
    internal class HttpListenerException : Exception
    {
        internal HttpListenerException(int errorCode, string message) 
            : base(message)
        {
            ErrorCode = errorCode;
        }
        
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public int ErrorCode { get; }
    }
}
using System;

#if !NET46
namespace Unosquare.Net
{
    /// <summary>
    /// Represents an HTTP Listener's exception
    /// </summary>
    public class HttpListenerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerException"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="message">The message.</param>
        public HttpListenerException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }
        
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public int ErrorCode { get; }
    }
}
#endif
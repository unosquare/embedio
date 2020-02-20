using System;
using System.Runtime.Serialization;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// The exception that is thrown when a <see cref="LockToken"/> has been locked.
    /// </summary>
    [Serializable]
    public class LockedException : InvalidOperationException
    {
        #region Instance management

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        public LockedException()
            : this(default(LockToken))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LockedException(string? message)
            : this(message, default(LockToken))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If this parameter is not <see langword="null"/>, the current exception is raised in a catch block
        /// that handles the inner exception.</param>
        public LockedException(string? message, Exception? innerException)
            : this(message, innerException, default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        /// <param name="token">A <see cref="LockToken"/> associated with the exception.</param>
        public LockedException(LockToken token)
            : base("The configuration of an object is locked and cannot be further changed.")
        {
            Token = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="token">A <see cref="LockToken"/> associated with the exception.</param>
        public LockedException(string? message, LockToken token)
            : base(message)
        {
            Token = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If this parameter is not <see langword="null"/>, the current exception is raised in a catch block
        /// that handles the inner exception.</param>
        /// <param name="token">A <see cref="LockToken"/> associated with the exception.</param>
        public LockedException(string? message, Exception? innerException, LockToken token)
            : base(message, innerException)
        {
            Token = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedException"/> class.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected LockedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets a <see cref="LockToken"/> associated with the locked configuration.
        /// </summary>
        public LockToken Token { get; }

        #endregion
    }
}
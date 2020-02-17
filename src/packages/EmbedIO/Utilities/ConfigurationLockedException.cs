using System;
using System.Runtime.Serialization;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// The exception that is thrown when trying to change the configuration of an object
    /// after it has been locked.
    /// </summary>
    [Serializable]
    public class ConfigurationLockedException : InvalidOperationException
    {
        #region Instance management

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        public ConfigurationLockedException()
            : this(default(ConfigurationLockToken))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ConfigurationLockedException(string? message)
            : this(message, default(ConfigurationLockToken))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If this parameter is not <see langword="null"/>, the current exception is raised in a catch block
        /// that handles the inner exception.</param>
        public ConfigurationLockedException(string? message, Exception? innerException)
            : this(message, innerException, default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        /// <param name="token">A <see cref="ConfigurationLockToken"/> associated with the locked configuration.</param>
        public ConfigurationLockedException(ConfigurationLockToken token)
            : base("The configuration of an object is locked and cannot be further changed.")
        {
            ConfigurationLockToken = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="token">A <see cref="ConfigurationLockToken"/> associated with the locked configuration.</param>
        public ConfigurationLockedException(string? message, ConfigurationLockToken token)
            : base(message)
        {
            ConfigurationLockToken = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.
        /// If this parameter is not <see langword="null"/>, the current exception is raised in a catch block
        /// that handles the inner exception.</param>
        /// <param name="token">A <see cref="ConfigurationLockToken"/> associated with the locked configuration.</param>
        public ConfigurationLockedException(string? message, Exception? innerException, ConfigurationLockToken token)
            : base(message, innerException)
        {
            ConfigurationLockToken = token;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationLockedException"/> class.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected ConfigurationLockedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets a <see cref="ConfigurationLockToken"/> associated with the locked configuration.
        /// </summary>
        public ConfigurationLockToken ConfigurationLockToken { get; }

        #endregion
    }
}
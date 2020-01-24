using System;
using System.Runtime.Serialization;

/*
 * NOTE TO CONTRIBUTORS:
 *
 * Never use this exception directly.
 * Use the methods in EmbedIO.Internal.SelfCheck instead.
 */

namespace EmbedIO
{
#pragma warning disable SA1642 // Constructor summary documentation should begin with standard text
    /// <summary>
    /// <para>The exception that is thrown by EmbedIO's internal diagnostic checks to signal a condition
    /// most probably caused by an error in EmbedIO.</para>
    /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
    /// </summary>
    [Serializable]
    public class EmbedIOInternalErrorException : Exception
    {
        /// <summary>
        /// <para>Initializes a new instance of the <see cref="EmbedIOInternalErrorException"/> class.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        public EmbedIOInternalErrorException()
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="EmbedIOInternalErrorException"/> class.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EmbedIOInternalErrorException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="EmbedIOInternalErrorException"/> class.</para>
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or <see langword="null"/> if no inner exception is specified.</param>
        public EmbedIOInternalErrorException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbedIOInternalErrorException"/> class.
        /// <para>This API supports the EmbedIO infrastructure and is not intended to be used directly from your code.</para>
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected EmbedIOInternalErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
#pragma warning restore SA1642
}
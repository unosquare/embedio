using System;
using System.Runtime.Serialization;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// The exception that is thrown when a conversion from a string to a
    /// specified type fails.
    /// </summary>
    /// <seealso cref="FromString" />
    [Serializable]
    public class StringConversionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringConversionException"/> class.
        /// </summary>
        public StringConversionException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringConversionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public StringConversionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringConversionException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or <see langword="null" /> if no inner exception is specified.</param>
        public StringConversionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringConversionException"/> class.
        /// </summary>
        /// <param name="type">The desired resulting type of the attempted conversion.</param>
        public StringConversionException(Type type)
            : base(BuildStandardMessageForType(type))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringConversionException"/> class.
        /// </summary>
        /// <param name="type">The desired resulting type of the attempted conversion.</param>
        /// <param name="innerException">The exception that is the cause of the current exception,
        /// or <see langword="null" /> if no inner exception is specified.</param>
        public StringConversionException(Type type, Exception innerException)
            : base(BuildStandardMessageForType(type), innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringConversionException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> that holds the serialized object data
        /// about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext" /> that contains contextual information
        /// about the source or destination.</param>
        protected StringConversionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string BuildStandardMessageForType(Type type)
            => $"Cannot convert a string to an instance of {type.FullName}";
    }
}
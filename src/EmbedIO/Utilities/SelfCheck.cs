using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Provides methods to perform self-checks in EmbedIO code.</para>
    /// <para>This API mainly supports the EmbedIO infrastructure; it is not intended
    /// to be used directly from your code, unless in EmbedIO plug-ins.</para>
    /// </summary>
    public static class SelfCheck
    {
        /// <summary>
        /// Creates an exception representing a self-check failure.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <returns>A newly-created instance of <see cref="EmbedIOInternalErrorException"/>.</returns>
        public static Exception Failure(string message)
            => new EmbedIOInternalErrorException(message);

        /// <summary>
        /// Creates an exception representing a self-check failure.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="exception">An exception related to the failure,
        /// or <see langword="null"/> if no such exception is specified.</param>
        /// <returns>A newly-created instance of <see cref="EmbedIOInternalErrorException"/>.</returns>
        public static Exception Failure(string message, Exception? exception)
            => new EmbedIOInternalErrorException(message, exception);

        /// <summary>
        /// Throws an <see cref="EmbedIOInternalErrorException"/>
        /// if a condition is not satisfied.
        /// </summary>
        /// <param name="condition">A boolean expression that, if <see langword="false"/>,
        /// indicates failure of the self-check..</param>
        /// <param name="message">The exception message to use
        /// if <paramref name="condition"/> is <see langword="false"/>.</param>
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new EmbedIOInternalErrorException(message);
        }
    }
}
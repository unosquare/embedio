using System;

namespace EmbedIO.Internal
{
    internal static class SelfCheck
    {
        public static Exception Failure(string message)
            => new EmbedIOInternalErrorException(message);

        public static Exception Failure(string message, Exception exception)
            => new EmbedIOInternalErrorException(message, exception);

        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new EmbedIOInternalErrorException(message);
        }
    }
}
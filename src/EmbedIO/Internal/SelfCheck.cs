using System;

namespace EmbedIO.Internal
{
    internal static class SelfCheck
    {
        public static void Fail(string message)
            => throw new EmbedIOInternalErrorException(message);

        public static void Fail(string message, Exception exception)
            => throw new EmbedIOInternalErrorException(message, exception);

        public static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new EmbedIOInternalErrorException(message);
        }
    }
}
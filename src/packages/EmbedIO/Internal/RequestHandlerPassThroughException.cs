using System;

namespace EmbedIO.Internal
{
// This exception is only created and handled internally,
// so it doesn't need all the standard the bells and whistles.
#pragma warning disable CA1032 // Add standard exception constructors
#pragma warning disable CA1064 // Exceptions should be public

    internal class RequestHandlerPassThroughException : Exception
    {
    }
#pragma warning restore CA1032
#pragma warning restore CA1064
}
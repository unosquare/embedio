using System;
using System.Linq;
using System.Threading;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="Exception"/>.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Returns a value that tells whether an <see cref="Exception"/> is of a type that
        /// we better not catch and ignore.
        /// </summary>
        /// <param name="this">The exception being thrown.</param>
        /// <returns><see langword="true"/> if <paramref name="this"/> is a critical exception;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsCriticalException(this Exception @this)
            => @this.IsCriticalExceptionCore()
            || (@this.InnerException?.IsCriticalException() ?? false)
            || (@this is AggregateException aggregateException && aggregateException.InnerExceptions.Any(e => e.IsCriticalException()));

        /// <summary>
        /// Returns a value that tells whether an <see cref="Exception"/> is of a type that
        /// will likely cause application failure.
        /// </summary>
        /// <param name="this">The exception being thrown.</param>
        /// <returns><see langword="true"/> if <paramref name="this"/> is a fatal exception;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool IsFatalException(this Exception @this)
            => @this.IsFatalExceptionCore()
            || (@this.InnerException?.IsFatalException() ?? false)
            || (@this is AggregateException aggregateException && aggregateException.InnerExceptions.Any(e => e.IsFatalException()));

        private static bool IsCriticalExceptionCore(this Exception @this)
            => IsFatalExceptionCore(@this)
            || @this is AppDomainUnloadedException
            || @this is BadImageFormatException
            || @this is CannotUnloadAppDomainException
            || @this is InvalidProgramException
            || @this is NullReferenceException
            || @this is IndexOutOfRangeException
            || @this is ArgumentException;

        private static bool IsFatalExceptionCore(this Exception @this)
            => @this is StackOverflowException
            || @this is OutOfMemoryException
            || @this is ThreadAbortException
            || @this is AccessViolationException;
    }
}
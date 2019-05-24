using System;
using System.Collections;
using System.Linq;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for types implementing <see cref="IEnumerable"/>.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the first element of a sequence that is of a given type,
        /// or a default value if the sequence contains no elements of the given type.
        /// </summary>
        /// <returns>
        /// <c>default(<typeparamref name="T"/>)</c> if <paramref name="this"/> is empty
        /// or contains no elements of type <typeparamref name="T"/>;
        /// otherwise, the first element in <paramref name="this" /> that is of type <typeparamref name="T"/>.
        /// </returns>
        /// <typeparam name="T">The type of the element to return.</typeparam>
        /// <param name="this">The <see cref="IEnumerable"/> on which this method is called.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static T FirstOrDefaultOfType<T>(this IEnumerable @this) => @this.OfType<T>().FirstOrDefault();

        /// <summary>
        /// Returns the first element of a sequence that is of a given type and satisfies a condition,
        /// or a default value if the sequence contains no elements of the given type
        /// that satisfy the condition.
        /// </summary>
        /// <returns>
        /// <c>default(<typeparamref name="T"/>)</c> if <paramref name="this"/> is empty
        /// or contains no elements of type <typeparamref name="T"/>;
        /// otherwise, the first element in <paramref name="this" /> that is of type <typeparamref name="T"/>.
        /// </returns>
        /// <typeparam name="T">The type of the element to return.</typeparam>
        /// <param name="this">The <see cref="IEnumerable"/> on which this method is called.</param>
        /// <param name="predicate">A function to test each element of type <typeparamref name="T"/> for a condition.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static T FirstOrDefaultOfType<T>(this IEnumerable @this, Func<T, bool> predicate)
            => @this.OfType<T>().FirstOrDefaultOfType(predicate);
    }
}
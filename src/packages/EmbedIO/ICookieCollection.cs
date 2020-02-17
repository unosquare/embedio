using System.Net;
using System.Collections;
using System.Collections.Generic;

namespace EmbedIO
{
    /// <summary>
    /// Interface for Cookie Collection.
    /// </summary>
    /// <seealso cref="ICollection" />
#pragma warning disable CA1010 // Should implement ICollection<Cookie> - not possible when wrapping System.Net.CookieCollection.
    public interface ICookieCollection : IEnumerable<Cookie>, ICollection
#pragma warning restore CA1010
    {
        /// <summary>
        /// Gets the <see cref="Cookie"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="Cookie"/>.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns>The cookie matching the specified name.</returns>
        Cookie? this[string name] { get; }

        /// <summary>
        /// Determines whether this <see cref="ICookieCollection"/> contains the specified <see cref="Cookie"/>.
        /// </summary>
        /// <param name="cookie">The cookie to find in the <see cref="ICookieCollection"/>.</param>
        /// <returns>
        /// <see langword="true"/> if this <see cref="ICookieCollection"/> contains the specified <paramref name="cookie"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        bool Contains(Cookie cookie);

        /// <summary>
        /// Copies the elements of this <see cref="ICookieCollection"/> to a <see cref="Cookie"/> array
        /// starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The target <see cref="Cookie"/> array to which the <see cref="ICookieCollection"/> will be copied.</param>
        /// <param name="index">The zero-based index in the target <paramref name="array"/> where copying begins.</param>
        void CopyTo(Cookie[] array, int index);

        /// <summary>
        /// Adds the specified cookie.
        /// </summary>
        /// <param name="cookie">The cookie.</param>
        void Add(Cookie cookie);
    }
}
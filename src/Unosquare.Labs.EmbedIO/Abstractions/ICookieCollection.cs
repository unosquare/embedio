namespace Unosquare.Labs.EmbedIO
{
    using System.Net;
    using System.Collections;

    /// <inheritdoc />
    /// <summary>
    /// Interface for Cookie Collection.
    /// </summary>
    /// <seealso cref="T:System.Collections.ICollection" />
    public interface ICookieCollection : ICollection
    {
        /// <summary>
        /// Adds the specified cookie.
        /// </summary>
        /// <param name="cookie">The cookie.</param>
        void Add(Cookie cookie);

        /// <summary>
        /// Gets the <see cref="Cookie"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="Cookie"/>.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns>The cookie matching the specified name.</returns>
        Cookie this[string name] { get; }
    }
}

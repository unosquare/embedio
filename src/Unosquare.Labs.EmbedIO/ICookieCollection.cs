namespace Unosquare.Labs.EmbedIO
{
    using System;
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
    }
}

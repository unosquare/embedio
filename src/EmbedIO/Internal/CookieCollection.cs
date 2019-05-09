using System;
using System.Collections;
using System.Net;

namespace EmbedIO.Internal
{
    /// <summary>
    /// Represents a wrapper for <c>System.Net.CookieCollection</c>.
    /// </summary>
    /// <seealso cref="ICookieCollection" />
    public class CookieCollection : ICookieCollection
    {
        private readonly System.Net.CookieCollection _cookieCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CookieCollection"/> class.
        /// </summary>
        /// <param name="cookieCollection">The cookie collection.</param>
        public CookieCollection(System.Net.CookieCollection cookieCollection)
        {
            _cookieCollection = cookieCollection;
        }
        
        /// <inheritdoc />
        public int Count => _cookieCollection.Count;

        /// <inheritdoc />
        public bool IsSynchronized => _cookieCollection.IsSynchronized;

        /// <inheritdoc />
        public object SyncRoot => _cookieCollection.SyncRoot;

        /// <inheritdoc />
        public Cookie this[string name] => _cookieCollection[name];

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => _cookieCollection.GetEnumerator();

        /// <inheritdoc />
        public void CopyTo(Array array, int index) => _cookieCollection.CopyTo(array, index);

        /// <inheritdoc />
        public void Add(Cookie cookie) => _cookieCollection.Add(cookie);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace EmbedIO.Net.Internal
{
    /// <summary>
    /// Represents a wrapper for <c>System.Net.CookieCollection</c>.
    /// </summary>
    /// <seealso cref="ICookieCollection" />
    internal sealed class SystemCookieCollection : ICookieCollection
    {
        private readonly CookieCollection _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCookieCollection"/> class.
        /// </summary>
        /// <param name="collection">The cookie collection.</param>
        public SystemCookieCollection(CookieCollection collection)
        {
            _collection = collection;
        }

        /// <inheritdoc />
        public int Count => _collection.Count;

        /// <inheritdoc />
        public bool IsSynchronized => _collection.IsSynchronized;

        /// <inheritdoc />
        public object SyncRoot => _collection.SyncRoot;

        /// <inheritdoc />
        public Cookie? this[string name] => _collection[name];

        /// <inheritdoc />
        IEnumerator<Cookie> IEnumerable<Cookie>.GetEnumerator() => _collection.OfType<Cookie>().GetEnumerator();

        /// <inheritdoc />
        public IEnumerator GetEnumerator() => _collection.GetEnumerator();

        /// <inheritdoc />
        public void CopyTo(Array array, int index) => _collection.CopyTo(array, index);

        /// <inheritdoc />
        public void CopyTo(Cookie[] array, int index) => _collection.CopyTo(array, index);

        /// <inheritdoc />
        public void Add(Cookie cookie) => _collection.Add(cookie);

        /// <inheritdoc />
        public bool Contains(Cookie cookie) => _collection.OfType<Cookie>().Contains(cookie);
    }
}
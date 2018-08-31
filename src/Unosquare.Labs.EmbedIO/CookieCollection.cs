﻿#if !NETSTANDARD1_3 && !UWP
namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections;
    using System.Net;

    /// <summary>
    /// Represetns a wrapper for <c>System.Net.CookieCollection</c>.
    /// </summary>
    /// <seealso cref="Unosquare.Labs.EmbedIO.ICookieCollection" />
    public class CookieCollection
        : ICookieCollection
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
        public IEnumerator GetEnumerator() => _cookieCollection.GetEnumerator();

        /// <inheritdoc />
        public void CopyTo(Array array, int index) => _cookieCollection.CopyTo(array, index);

        /// <inheritdoc />
        public int Count => _cookieCollection.Count;

        /// <inheritdoc />
        public bool IsSynchronized => _cookieCollection.IsSynchronized;

        /// <inheritdoc />
        public object SyncRoot => _cookieCollection.SyncRoot;

        /// <inheritdoc />
        public void Add(Cookie cookie) => _cookieCollection.Add(cookie);
    }
}
#endif
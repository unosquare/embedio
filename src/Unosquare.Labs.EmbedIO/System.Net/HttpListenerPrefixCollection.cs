#if !NET46
//
// System.Net.HttpListenerPrefixCollection.cs
//
// Author:
//	Gonzalo Paniagua Javier (gonzalo@novell.com)
//
// Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Unosquare.Net
{
    /// <summary>
    /// Represents a collection of HTTP listener profixes
    /// </summary>
    public class HttpListenerPrefixCollection : ICollection<string>
    {
        readonly List<string> _prefixes = new List<string>();

        readonly HttpListener _listener;

        internal HttpListenerPrefixCollection(HttpListener listener)
        {
            _listener = listener;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public int Count => _prefixes.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets a value indicating whether this instance is synchronized.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is synchronized; otherwise, <c>false</c>.
        /// </value>
        public bool IsSynchronized => false;

        /// <summary>
        /// Adds the specified URI prefix.
        /// </summary>
        /// <param name="uriPrefix">The URI prefix.</param>
        public void Add(string uriPrefix)
        {
            _listener.CheckDisposed();
            ListenerPrefix.CheckUri(uriPrefix);
            if (_prefixes.Contains(uriPrefix))
                return;

            _prefixes.Add(uriPrefix);
            if (_listener.IsListening)
                EndPointManager.AddPrefix(uriPrefix, _listener);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            _listener.CheckDisposed();
            _prefixes.Clear();
            if (_listener.IsListening)
                EndPointManager.RemoveListener(_listener);
        }

        /// <summary>
        /// Determines whether [contains] [the specified URI prefix].
        /// </summary>
        /// <param name="uriPrefix">The URI prefix.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified URI prefix]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string uriPrefix)
        {
            _listener.CheckDisposed();
            return _prefixes.Contains(uriPrefix);
        }

        /// <summary>
        /// Copies the prefixes to the specified string array
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The offset.</param>
        public void CopyTo(string[] array, int offset)
        {
            _listener.CheckDisposed();
            _prefixes.CopyTo(array, offset);
        }

        /// <summary>
        /// Copies the prefixes to the specified string
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="offset">The offset.</param>
        public void CopyTo(Array array, int offset)
        {
            _listener.CheckDisposed();
            ((ICollection)_prefixes).CopyTo(array, offset);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<string> GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _prefixes.GetEnumerator();
        }

        /// <summary>
        /// Removes the specified URI prefix.
        /// </summary>
        /// <param name="uriPrefix">The URI prefix.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">uriPrefix</exception>
        public bool Remove(string uriPrefix)
        {
            _listener.CheckDisposed();
            if (uriPrefix == null)
                throw new ArgumentNullException(nameof(uriPrefix));

            var result = _prefixes.Remove(uriPrefix);
            if (result && _listener.IsListening)
                EndPointManager.RemovePrefix(uriPrefix, _listener);

            return result;
        }
    }
}
#endif
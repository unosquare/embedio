#if !NET47
namespace Unosquare.Net
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a collection of HTTP listener prefixes.
    /// </summary>
    public class HttpListenerPrefixCollection : List<string>
    {
        private readonly HttpListener _listener;

        internal HttpListenerPrefixCollection(HttpListener listener)
        {
            _listener = listener;
        }
        
        /// <summary>
        /// Adds the specified URI prefix.
        /// </summary>
        /// <param name="uriPrefix">The URI prefix.</param>
        public new void Add(string uriPrefix)
        {
            ListenerPrefix.CheckUri(uriPrefix);
            if (Contains(uriPrefix))
                return;

            base.Add(uriPrefix);
            if (_listener.IsListening)
                EndPointManager.AddPrefix(uriPrefix, _listener);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public new void Clear()
        {
            base.Clear();

            if (_listener.IsListening)
                EndPointManager.RemoveListener(_listener);
        }

        /// <summary>
        /// Removes the specified URI prefix.
        /// </summary>
        /// <param name="uriPrefix">The URI prefix.</param>
        /// <returns>True if "uriPrefix" was removed; otherwise, false.</returns>
        /// <exception cref="System.ArgumentNullException">uriPrefix.</exception>
        public new bool Remove(string uriPrefix)
        {
            if (uriPrefix == null)
                throw new ArgumentNullException(nameof(uriPrefix));

            var result = base.Remove(uriPrefix);
            if (result && _listener.IsListening)
                EndPointManager.RemovePrefix(uriPrefix, _listener);

            return result;
        }
    }
}
#endif
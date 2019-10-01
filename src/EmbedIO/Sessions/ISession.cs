using System;
using System.Collections.Generic;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// Represents a session.
    /// </summary>
    public interface ISession
    {
        /// <summary>
        /// A unique identifier for the session.
        /// </summary>
        /// <value>The unique identifier for this session.</value>
        /// <seealso cref="Session.IdComparison"/>
        /// <seealso cref="Session.IdComparer"/>
        string Id { get; }

        /// <summary>
        /// Gets the time interval, starting from <see cref="LastActivity"/>,
        /// after which the session expires.
        /// </summary>
        /// <value> The expiration time.</value>
        TimeSpan Duration { get; }

        /// <summary>
        /// Gets the UTC date and time of last activity on the session.
        /// </summary>
        /// <value>
        /// The UTC date and time of last activity on the session.
        /// </value>
        DateTime LastActivity { get; }

        /// <summary>
        /// Gets the number of key/value pairs contained in a session.
        /// </summary>
        /// <value>
        /// The number of key/value pairs contained in the object that implements <see cref="ISession"/>.
        /// </value>
        int Count { get; }

        /// <summary>
        /// Gets a value that indicates whether a session is empty.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the object that implements <see cref="ISession"/> is empty,
        /// i.e. contains no key / value pairs; otherwise, <see langword="false"/>.
        /// </value>
        bool IsEmpty { get; }

        /// <summary>
        /// <para>Gets or sets the value associated with the specified key.</para>
        /// <para>Note that a session does not store null values; therefore, setting this property to <see langword="null"/>
        /// has the same effect as removing <paramref name="key"/> from the dictionary.</para>
        /// </summary>
        /// <value>
        /// The value associated with the specified key, if <paramref name="key"/>
        /// is found in the dictionary; otherwise, <see langword="null"/>.
        /// </value>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        object this[string key] { get; set; }

        /// <summary>
        /// Removes all keys and values from a session.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether a session contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the object that implements <see cref="ISession"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the object that implements <see cref="ISession"/> contains an element with the key;
        /// otherwise, <see langword="false"/> .
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        bool ContainsKey(string key);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>,
        /// if the key is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the object that implements <see cref="ISession"/>
        /// contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        bool TryGetValue(string key, out object value);

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from a session.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, the value removed from the object that implements <see cref="ISession"/>,
        /// if the key is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the value was removed successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        bool TryRemove(string key, out object value);

        /// <summary>
        /// Takes and returns a snapshot of the contents of a session at the time of calling.
        /// </summary>
        /// <returns>An <see cref="IReadOnlyList{T}">IReadOnlyList&lt;KeyValuePair&lt;string,object&gt;&gt;</see> interface
        /// containing an immutable copy of the session data as it was at the time of calling this method.</returns>
        /// <remarks>
        /// <para>The objects contained in the session data are copied by reference, not cloned; therefore
        /// you should be aware that their state may change even after the snapshot is taken.</para>
        /// </remarks>
        IReadOnlyList<KeyValuePair<string, object>> TakeSnapshot();
    }
}
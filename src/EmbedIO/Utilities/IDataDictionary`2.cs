using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Represents a generic collection of key/value pairs that does not store
    /// null values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. This must be a reference type.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary. This must be a reference type.</typeparam>
    /// <remarks>
    /// <para>This interface is meant for the storage and retrieval of session data,
    /// not for all possible uses of a generic dictionary; therefore, some features
    /// (e.g. key/value pair enumeration) are intentionally not included.</para>
    /// </remarks>
    public interface IDataDictionary<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="IDataDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <value>
        /// The number of key/value pairs contained in the <see cref="IDataDictionary{TKey,TValue}"/>.
        /// </value>
        int Count { get; }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="IDataDictionary{TKey,TValue}"/> is empty.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the <see cref="IDataDictionary{TKey,TValue}"/> is empty; otherwise, <see langword="false"/>.
        /// </value>
        bool IsEmpty { get; }

        /// <summary>
        /// <para>Gets or sets the value associated with the specified key.</para>
        /// <para>Note that a <see cref="IDataDictionary{TKey,TValue}"/> does not store
        /// null values>; therefore, setting this property to <see langword="null"/>
        /// has the same effect as removing <paramref name="key"/> from the dictionary.</para>
        /// </summary>
        /// <value>
        /// The value associated with the specified key, if <paramref name="key"/>
        /// is found in the dictionary; otherwise, <see langword="null"/>.
        /// </value>
        /// <param name="key">The key of the value to get or set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        TValue this[TKey key] { get; set; }

        /// <summary>
        /// Removes all keys and values from the <see cref="IDataDictionary{TKey,TValue}"/>.
        /// </summary>
        void Clear();

        /// <summary>
        /// Determines whether the <see cref="IDataDictionary{TKey,TValue}"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="IDataDictionary{TKey,TValue}"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IDataDictionary{TKey,TValue}"/> contains an element with the key; otherwise, <see langword="false"/> .
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>,
        /// if the key is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the object that implements <see cref="IDataDictionary{TKey,TValue}"/>
        /// contains an element with the specified key; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the <see cref="IDataDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="key">The key of the element to remove and return.</param>
        /// <param name="value">When this method returns, the value removed from the <see cref="IDataDictionary{TKey,TValue}"/>,
        /// if the key is found; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the value was removed successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
        bool TryRemove(TKey key, out TValue value);
    }
}
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Represents a non-thread-safe collection of key/value pairs that does not store null values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary. This must be a reference type.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary. This must be a reference type.</typeparam>
    /// <seealso cref="IDataDictionary{TKey,TValue}"/>
    public sealed class DataDictionary<TKey, TValue> : IDataDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        #region Private data

        private readonly Dictionary<TKey, TValue> _dictionary;

        #endregion

        #region Instance management

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDictionary{TKey,TValue}"/> class
        /// that is empty, has the default initial capacity,
        /// and uses the default comparer for <typeparamref name="TKey"/>.
        /// </summary>
        /// <see cref="Dictionary{TKey,TValue}()"/>
        public DataDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDictionary{TKey,TValue}"/> class
        /// that contains elements copied from the specified <see cref="IEnumerable{T}"/>,
        /// has the default initial capacity, and uses the default comparer for <typeparamref name="TKey"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied
        /// to the new <see cref="DataDictionary{TKey,TValue}"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>Since <see cref="DataDictionary{TKey,TValue}"/> does not store null values,
        /// key/value pairs whose value is <see langword="null"/> will not be copied from <paramref name="collection"/>.</para>
        /// </remarks>
        /// <see cref="Dictionary{TKey,TValue}()"/>
        public DataDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            Validate.NotNull(nameof(collection), collection);

            _dictionary = new Dictionary<TKey, TValue>();
            foreach (var pair in collection.Where(pair => pair.Value != null))
            {
                _dictionary.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDictionary{TKey,TValue}"/> class
        /// that is empty, has the default capacity, and uses the specified <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="comparer">The equality comparison implementation to use when comparing keys.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <see langword="null"/>.</exception>
        /// <see cref="Dictionary{TKey,TValue}(IEqualityComparer{TKey})"/>
        public DataDictionary(IEqualityComparer<TKey> comparer)
        {
            _dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDictionary{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="IEnumerable{T}"/>,
        /// has the default initial capacity, and uses the specified <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied
        /// to the new <see cref="DataDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The equality comparison implementation to use when comparing keys.</param>
        /// <remarks>
        /// <para>Since <see cref="DataDictionary{TKey,TValue}"/> does not store null values,
        /// key/value pairs whose value is <see langword="null"/> will not be copied from <paramref name="collection"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="collection"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="comparer"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <see cref="Dictionary{TKey,TValue}(IEqualityComparer{TKey})"/>
        public DataDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            Validate.NotNull(nameof(collection), collection);

            _dictionary = new Dictionary<TKey, TValue>(comparer);
            foreach (var pair in collection.Where(pair => pair.Value != null))
            {
                _dictionary.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDictionary{TKey, TValue}"/> class
        /// that is empty, has the specified capacity, and uses the default comparer for the key type.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="DataDictionary{TKey, TValue}"/> can contain.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        /// <see cref="Dictionary{TKey,TValue}(int)"/>
        public DataDictionary(int capacity)
        {
            _dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDictionary{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="IEnumerable{T}"/>, 
        /// has the specified capacity, and uses the specified <see cref="IEqualityComparer{T}"/>.
        /// </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="DataDictionary{TKey, TValue}"/> can contain.</param>
        /// <param name="collection">The <see cref="IEnumerable{T}"/> whose elements are copied
        /// to the new <see cref="ConcurrentDataDictionary{TKey,TValue}"/>.</param>
        /// <param name="comparer">The equality comparison implementation to use when comparing keys.</param>
        /// <remarks>
        /// <para>Since <see cref="ConcurrentDataDictionary{TKey,TValue}"/> does not store null values,
        /// key/value pairs whose value is <see langword="null"/> will not be copied from <paramref name="collection"/>.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="collection"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="comparer"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than 0.</exception>
        /// <see cref="Dictionary{TKey,TValue}(int,IEqualityComparer{TKey})"/>
        public DataDictionary(int capacity, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
        {
            Validate.NotNull(nameof(collection), collection);

            _dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
            foreach (var pair in collection.Where(pair => pair.Value != null))
            {
                _dictionary.Add(pair.Key, pair.Value);
            }
        }

        #endregion

        #region Public APIs

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.Count"/>
        public int Count => _dictionary.Count;

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.IsEmpty"/>
        public bool IsEmpty => _dictionary.Count == 0;

        /// <inheritdoc cref="IDictionary{TKey,TValue}.Keys"/>
        public ICollection<TKey> Keys => _dictionary.Keys;

        /// <inheritdoc cref="IDictionary{TKey,TValue}.Values"/>
        public ICollection<TValue> Values => _dictionary.Values;

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.this"/>
        public TValue this[TKey key]
        {
            get => _dictionary.TryGetValue(Validate.NotNull(nameof(key), key), out var value) ? value : null;
            set
            {
                if (value != null)
                {
                    _dictionary[key] = value;
                }
                else
                {
                    _dictionary.Remove(key);
                }
            }
        }

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.Clear"/>
        public void Clear() => _dictionary.Clear();

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.ContainsKey"/>
        public bool ContainsKey(TKey key)
        {
            // _dictionary.ContainsKey will take care of throwing on a null key.
            return _dictionary.ContainsKey(key);
        }

        /// <inheritdoc cref="ConcurrentDictionary{TKey,TValue}.GetOrAdd(TKey,TValue)"/>
        public TValue GetOrAdd(TKey key, TValue value)
        {
            // _dictionary.TryGetValue will take care of throwing on a null key.
            if (_dictionary.TryGetValue(key, out var result))
                return result;

            if (value == null)
                return null;

            _dictionary.Add(key, value);
            return value;
        }

        /// <inheritdoc cref="IDictionary{TKey,TValue}.Remove(TKey)"/>
        public bool Remove(TKey key)
        {
            // _dictionary.Remove will take care of throwing on a null key.
            return _dictionary.Remove(key);
        }

        /// <inheritdoc cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/>
        public bool TryAdd(TKey key, TValue value)
        {
            // _dictionary.ContainsKey will take care of throwing on a null key.
            if (_dictionary.ContainsKey(key))
                return false;

            if (value != null)
                _dictionary.Add(key, value);

            return true;
        }

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.TryGetValue"/>
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

        /// <inheritdoc cref="IDataDictionary{TKey,TValue}.TryRemove"/>
        public bool TryRemove(TKey key, out TValue value)
        {
            // TryGetValue will take care of throwing on a null key.
            if (!_dictionary.TryGetValue(key, out value))
                return false;

            _dictionary.Remove(key);
            return true;
        }

        /// <inheritdoc cref="ConcurrentDictionary{TKey,TValue}.TryUpdate"/>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            // TryGetValue will take care of throwing on a null key.
            if (!_dictionary.TryGetValue(key, out var value))
                return false;

            if (value != comparisonValue)
                return false;

            _dictionary[key] = newValue;
            return true;
        }

        #endregion

        #region Implementation of IDictionary<TKey, TValue>

        /// <inheritdoc cref="IDictionary{TKey,TValue}.Add(TKey,TValue)"/>
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            // Validating the key seems redundant, because both Add and Remove
            // will throw on a null key.
            // This way, though, the code path on null key does not depend on value.
            // Without this validation, there should be two unit tests for null key,
            // one with a null value and one with a non-null value,
            // which makes no sense.
            Validate.NotNull(nameof(key), key);

            if (value != null)
            {
                _dictionary.Add(key, value);
            }
            else
            {
                _dictionary.Remove(key);
            }
        }

        #endregion

        #region Implementation of IReadOnlyDictionary<TKey, TValue>

        /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.Keys"/>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _dictionary.Keys;

        /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.Values"/>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _dictionary.Values;

        #endregion

        #region Implementation of ICollection<KeyValuePair<TKey, TValue>>

        /// <inheritdoc cref="ICollection{T}.IsReadOnly"/>
        /// <remarks>
        /// <para>This property is always <see langword="false"/> for a <see cref="DataDictionary{TKey,TValue}"/>.</para>
        /// </remarks>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        /// <inheritdoc cref="ICollection{T}.Add"/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            if (item.Value != null)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Add(item);
            }
            else
            {
                _dictionary.Remove(item.Key);
            }
        }

        /// <inheritdoc cref="ICollection{T}.Contains"/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Contains(item);

        /// <inheritdoc cref="ICollection{T}.CopyTo"/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

        /// <inheritdoc cref="ICollection{T}.Remove"/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => ((ICollection<KeyValuePair<TKey, TValue>>) _dictionary).Remove(item);

        #endregion

        #region Implementation of IEnumerable<KeyValuePair<TKey, TValue>>

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _dictionary.GetEnumerator();

        #endregion

        #region Implementation of IEnumerable

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();

        #endregion
    }
}
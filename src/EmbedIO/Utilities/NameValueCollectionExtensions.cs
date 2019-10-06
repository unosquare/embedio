using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="NameValueCollection"/>.
    /// </summary>
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// <para>Converts a <see cref="NameValueCollection"/> to a dictionary of objects.</para>
        /// <para>Values in the returned dictionary will wither be strings, or arrays of strings,
        /// depending on the presence of multiple values for the same key in the collection.</para>
        /// </summary>
        /// <param name="this">The <see cref="NameValueCollection"/> on which this method is called.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> associating the collection's keys
        /// with their values.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static Dictionary<string, object?> ToDictionary(this NameValueCollection @this)
            => @this.Keys.Cast<string>().ToDictionary(key => key, key => {
                var values = @this.GetValues(key);
                if (values == null)
                    return null;

                return values.Length switch {
                    0 => null,
                    1 => (object) values[0],
                    _ => (object) values
                };
            });

        /// <summary>
        /// Converts a <see cref="NameValueCollection"/> to a dictionary of strings.
        /// </summary>
        /// <param name="this">The <see cref="NameValueCollection"/> on which this method is called.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> associating the collection's keys
        /// with their values (or comma-separated lists in case of multiple values).</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static Dictionary<string, string> ToStringDictionary(this NameValueCollection @this)
            => @this.Keys.Cast<string>().ToDictionary(key => key, @this.Get);

        /// <summary>
        /// Converts a <see cref="NameValueCollection"/> to a dictionary of arrays of strings.
        /// </summary>
        /// <param name="this">The <see cref="NameValueCollection"/> on which this method is called.</param>
        /// <returns>A <see cref="Dictionary{TKey,TValue}"/> associating the collection's keys
        /// with arrays of their values.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static Dictionary<string, string[]> ToArrayDictionary(this NameValueCollection @this)
            => @this.Keys.Cast<string>().ToDictionary(key => key, @this.GetValues);

        /// <summary>
        /// Determines whether a <see cref="NameValueCollection"/> contains one or more values
        /// for the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="this">The <see cref="NameValueCollection"/> on which this method is called.</param>
        /// <param name="key">The key to look for.</param>
        /// <returns><see langword="true"/> if at least one value for <paramref name="key"/>
        /// is present in the collection; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static bool ContainsKey(this NameValueCollection @this, string key)
            => @this.Keys.Cast<string>().Contains(key);

        /// <summary>
        /// Determines whether a <see cref="NameValueCollection"/> contains one or more values
        /// for the specified <paramref name="name"/>, at least one of which is equal to the specified
        /// <paramref name="value"/>. Value comparisons are carried out using the 
        /// <see cref="StringComparison.OrdinalIgnoreCase"/> comparison type.
        /// </summary>
        /// <param name="this">The <see cref="NameValueCollection"/> on which this method is called.</param>
        /// <param name="name">The name to look for.</param>
        /// <param name="value">The value to look for.</param>
        /// <returns><see langword="true"/> if at least one of the values for <paramref name="name"/>
        /// in the collection is equal to <paramref name="value"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <remarks>White space is trimmed from the start and end of each value before comparison.</remarks>
        /// <seealso cref="Contains(NameValueCollection,string,string,StringComparison)"/>
        public static bool Contains(this NameValueCollection @this, string name, string value)
            => Contains(@this, name, value, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether a <see cref="NameValueCollection"/> contains one or more values
        /// for the specified <paramref name="name"/>, at least one of which is equal to the specified
        /// <paramref name="value"/>. Value comparisons are carried out using the specified
        /// <paramref name="comparisonType"/>.
        /// </summary>
        /// <param name="this">The <see cref="NameValueCollection"/> on which this method is called.</param>
        /// <param name="name">The name to look for.</param>
        /// <param name="value">The value to look for.</param>
        /// <param name="comparisonType">One of the <see cref="StringComparison"/> enumeration values
        /// that specifies how the strings will be compared.</param>
        /// <returns><see langword="true"/> if at least one of the values for <paramref name="name"/>
        /// in the collection is equal to <paramref name="value"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <remarks>White space is trimmed from the start and end of each value before comparison.</remarks>
        /// <seealso cref="Contains(NameValueCollection,string,string)"/>
        public static bool Contains(this NameValueCollection @this, string name, string? value, StringComparison comparisonType)
        {
            value = value?.Trim();
            return @this[name]?.SplitByComma()
               .Any(val => string.Equals(val?.Trim(), value, comparisonType)) ?? false;
        }
    }
}
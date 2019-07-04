using System;
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
        public static bool Contains(this NameValueCollection @this, string name, string value, StringComparison comparisonType)
        {
            value = value?.Trim();
            return @this[name]?.SplitByComma()
               .Any(val => string.Equals(val?.Trim(), value, comparisonType)) ?? false;
        }
    }
}
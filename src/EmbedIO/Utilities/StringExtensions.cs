using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly char[] CommaSplitChars = {','};

        /// <summary>Splits a string into substrings based on the specified <paramref name="delimiters"/>.
        /// The returned array includes empty array elements if two or more consecutive delimiters are found
        /// in <paramref name="this"/>.</summary>
        /// <param name="this">The <see cref="string"/> on which this method is called.</param>
        /// <param name="delimiters">An array of <see cref="char"/>s to use as delimiters.</param>
        /// <returns>An array whose elements contain the substrings in <paramref name="this"/> that are delimited
        /// by one or more characters in <paramref name="delimiters"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        public static string[] SplitByAny(this string @this, params char[] delimiters) => @this.Split(delimiters);
        
        /// <summary>Splits a string into substrings, using the comma (<c>,</c>) character as a delimiter.
        /// The returned array includes empty array elements if two or more commas are found in <paramref name="this"/>.</summary>
        /// <param name="this">The <see cref="string"/> on which this method is called.</param>
        /// <returns>An array whose elements contain the substrings in <paramref name="this"/> that are delimited by commas.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="SplitByComma(string,StringSplitOptions)"/>
        public static string[] SplitByComma(this string @this) => @this.Split(CommaSplitChars);

        /// <summary>Splits a string into substrings, using the comma (<c>,</c>) character as a delimiter.
        /// You can specify whether the substrings include empty array elements.</summary>
        /// <param name="this">The <see cref="string"/> on which this method is called.</param>
        /// <param name="options"><see cref="StringSplitOptions.RemoveEmptyEntries"/> to omit empty array elements from the array returned;
        /// or <see cref="StringSplitOptions.None"/> to include empty array elements in the array returned.</param>
        /// <returns>
        /// <para>An array whose elements contain the substrings in <paramref name="this"/> that are delimited by commas.</para>
        /// <para>For more information, see the Remarks section of the <see cref="string.Split(char[],StringSplitOptions)"/> method.</para>
        /// </returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options">options</paramref> is not one of the <see cref="StringSplitOptions"/> values.</exception>
        /// <seealso cref="SplitByComma(string)"/>
        public static string[] SplitByComma(this string @this, StringSplitOptions options) =>
            @this.Split(CommaSplitChars, options);

        /// <summary>
        /// Ensures that a <see cref="string"/> is never empty,
        /// by transforming empty strings into <see langword="null"/>.
        /// </summary>
        /// <param name="this">The <see cref="string"/> on which this method is called.</param>
        /// <returns>If <paramref name="this"/> is the empty string, <see langword="null"/>;
        /// otherwise, <paramref name="this."/></returns>
        public static string? NullIfEmpty(this string @this)
            => string.IsNullOrEmpty(@this) ? null : @this;
    }
}
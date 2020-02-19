using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides extension methods for <see cref="string"/>.
    /// </summary>
    public static class StringExtensions
    {
        private static readonly char[] CommaSplitChars = { ',' };

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

        /// <summary>Retrieves a substring from this instance, with leading and trailing white-space characters removed.
        /// The substring starts at a specified character position and continues to the end of the string.</summary>
        /// <param name="this">The <see cref="string"/> on which this method is called.</param>
        /// <param name="startIndex">The zero-based starting character position of the substring to retrieve.</param>
        /// <returns>A string that is equivalent to the substring that begins at <paramref name="startIndex"/>
        /// in <paramref name="this"/>, minus any leading and/or trailing white-space characters.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="startIndex"/> is less than 0.</para>
        /// <para>- or -</para>
        /// <para><paramref name="startIndex"/> is larger then the length of <paramref name="this"/>.</para>
        /// </exception>
        public static string TrimmedSubstring(this string @this, int startIndex)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "The starting index of a substring must be non-negative.");

            if (startIndex > @this.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "The starting index of a substring cannot be larger than the string's length.");

            return TrimmedSubstringInternal(@this, startIndex, @this.Length - startIndex);
        }

        /// <summary>Retrieves a substring from this instance, with leading and trailing white-space characters removed.
        /// The substring starts at a specified character position and has a specified length.</summary>
        /// <param name="this">The <see cref="string"/> on which this method is called.</param>
        /// <param name="startIndex">The zero-based starting character position of the substring to retrieve.</param>
        /// <param name="length">The length of the substring to retrieve, including leading and/or trailing white-space characters.</param>
        /// <returns>A string that is equivalent to the substring of length <paramref name="length"/> that begins at <paramref name="startIndex"/>
        /// in <paramref name="this"/>, minus any leading and/or trailing white-space characters.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="startIndex"/> is less than 0.</para>
        /// <para>- or -</para>
        /// <para><paramref name="startIndex"/> is larger then the length of <paramref name="this"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="length"/> is less than 0.</para>
        /// <para>- or -</para>
        /// <para><paramref name="startIndex"/> is larger then the length of <paramref name="this"/>.</para>
        /// </exception>
        public static string TrimmedSubstring(this string @this, int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "The starting index of a substring must be non-negative.");

            if (startIndex > @this.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "The starting index of a substring cannot be larger than the string's length.");

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "The length of a substring cannot be negative.");

            if (startIndex > @this.Length - length)
                throw new ArgumentOutOfRangeException(nameof(length), "The starting index and length of a substring must refer to a location within the string.");

            return TrimmedSubstringInternal(@this, startIndex, length);
        }

        private static string TrimmedSubstringInternal(string @this, int startIndex, int length)
        {
            if (length == 0)
                return string.Empty;

            if (startIndex == 0 && length == @this.Length)
                return @this.Trim();

            int endIndex = startIndex + length - 1;

            while (startIndex <= endIndex && char.IsWhiteSpace(@this[startIndex]))
                startIndex++;

            while (endIndex >= startIndex && char.IsWhiteSpace(@this[endIndex]))
                endIndex--;

            int substringLength = endIndex - startIndex + 1;
            return
                substringLength == 0 ? string.Empty :
                substringLength == @this.Length ? @this :
                @this.Substring(startIndex, substringLength);
        }
    }
}
using System;
using System.IO;
using System.Security;

namespace EmbedIO.Utilities
{
    partial class Validate
    {
        private static readonly char[] InvalidLocalPathChars = GetInvalidLocalPathChars();

        /// <summary>
        /// Ensures that the value of an argument is a valid URL path
        /// and normalizes it.
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="isBasePath">If set to <see langword="true"/><c>true</c>, the returned path
        /// is ensured to end in a slash (<c>/</c>) character; otherwise, the returned path is
        /// ensured to not end in a slash character unless it is <c>"/"</c>.</param>
        /// <returns>The normalized URL path.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> does not start with a slash (<c>/</c>) character.</para>
        /// </exception>
        /// <seealso cref="Utilities.UrlPath.Normalize"/>
        public static string UrlPath(string argumentName, string value, bool isBasePath)
        {
            var exception = Utilities.UrlPath.ValidateInternal(argumentName, value);
            if (exception != null)
                throw exception;

            return Utilities.UrlPath.Normalize(value, isBasePath);
        }

        /// <summary>
        /// Ensures that the value of an argument is a valid local path
        /// and, optionally, gets the corresponding full path.
        /// </summary>
        /// <param name="argumentName">The name of the argument to validate.</param>
        /// <param name="value">The value to validate.</param>
        /// <param name="getFullPath"><see langword="true"/> to get the full path, <see langword="false"/> to leave the path as is..</param>
        /// <returns>The local path, or the full path if <paramref name="getFullPath"/> is <see langword="true"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="value"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains only white space.</para>
        /// <para>- or -</para>
        /// <para><paramref name="value"/> contains one or more invalid characters.</para>
        /// <para>- or -</para>
        /// <para><paramref name="getFullPath"/> is <see langword="true"/> and the full path could not be obtained.</para>
        /// </exception>
        public static string LocalPath(string argumentName, string value, bool getFullPath)
        {
            if (value == null)
                throw new ArgumentNullException(argumentName);

            if (value.Length == 0)
                throw new ArgumentException("Local path is empty.", argumentName);

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Local path contains only white space.", argumentName);

            if (value.IndexOfAny(InvalidLocalPathChars) >= 0)
                throw new ArgumentException("Local path contains one or more invalid characters.", argumentName);

            if (getFullPath)
            {
                try
                {
                    value = Path.GetFullPath(value);
                }
                catch (Exception e) when (e is ArgumentException || e is SecurityException || e is NotSupportedException || e is PathTooLongException)
                {
                    throw new ArgumentException("Could not get the full local path.", argumentName, e);
                }
            }

            return value;
        }

        private static char[] GetInvalidLocalPathChars()
        {
            var systemChars = Path.GetInvalidPathChars();
            var p = systemChars.Length;
            var result = new char[p + 2];
            Array.Copy(systemChars, result, p);
            result[p++] = '*';
            result[p] = '?';
            return result;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Provides utility methods to work with URL paths.
    /// </summary>
    public static class UrlPath
    {
        /// <summary>
        /// The root URL path value, i.e. <c>"/"</c>.
        /// </summary>
        public const string Root = "/";

        private static readonly Regex MultipleSlashRegex = new Regex("//+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Determines whether a string is a valid URL path.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <returns>
        /// <see langword="true"/> if the specified URL path is valid; otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>For a string to be a valid URL path, it must not be <see langword="null"/>,
        /// must not be empty, and must start with a slash (<c>/</c>) character.</para>
        /// <para>To ensure that a method parameter is a valid URL path, use <see cref="Validate.UrlPath"/>.</para>
        /// </remarks>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="UnsafeNormalize"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static bool IsValid(string path) => ValidateInternal(nameof(path), path) == null;

        /// <summary>
        /// Normalizes the specified URL path.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <param name="isBasePath">if set to <see langword="true"/>, treat the URL path
        /// as a base path, i.e. ensure it ends with a slash (<c>/</c>) character;
        /// otherwise, ensure that it does NOT end with a slash character.</param>
        /// <returns>The normalized path.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is not a valid URL path.
        /// </exception>
        /// <remarks>
        /// <para>A normalized URL path is one where each run of two or more slash
        /// (<c>/</c>) characters has been replaced with a single slash character.</para>
        /// <para>This method does NOT try to decode URL-encoded characters.</para>
        /// <para>If you are sure that <paramref name="path"/> is a valid URL path,
        /// for example because you have called <see cref="IsValid"/> and it returned
        /// <see langword="true"/>, then you may call <see cref="UnsafeNormalize"/>
        /// instead of this method. <see cref="UnsafeNormalize"/> is slightly faster because
        /// it skips the initial validity check.</para>
        /// <para>There is no need to call this method for a method parameter
        /// for which you have already called <see cref="Validate.UrlPath"/>.</para>
        /// </remarks>
        /// <seealso cref="UnsafeNormalize"/>
        /// <seealso cref="IsValid"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static string Normalize(string path, bool isBasePath)
        {
            var exception = ValidateInternal(nameof(path), path);
            if (exception != null)
                throw exception;

            return UnsafeNormalize(path, isBasePath);
        }

        /// <summary>
        /// Normalizes the specified URL path, assuming that it is valid.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <param name="isBasePath">if set to <see langword="true"/>, treat the URL path
        /// as a base path, i.e. ensure it ends with a slash (<c>/</c>) character;
        /// otherwise, ensure that it does NOT end with a slash character.</param>
        /// <returns>The normalized path.</returns>
        /// <remarks>
        /// <para>A normalized URL path is one where each run of two or more slash
        /// (<c>/</c>) characters has been replaced with a single slash character.</para>
        /// <para>This method does NOT try to decode URL-encoded characters.</para>
        /// <para>If <paramref name="path"/> is not valid, the behavior of
        /// this method is unspecified. You should call this method only after
        /// <see cref="IsValid"/> has returned <see langword="true"/>
        /// for the same <paramref name="path"/>.</para>
        /// <para>You should call <see cref="Normalize"/> instead of this method
        /// if you are not sure that <paramref name="path"/> is valid.</para>
        /// <para>There is no need to call this method for a method parameter
        /// for which you have already called <see cref="Validate.UrlPath"/>.</para>
        /// </remarks>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="IsValid"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static string UnsafeNormalize(string path, bool isBasePath)
        {
            // Replace each run of multiple slashes with a single slash
            path = MultipleSlashRegex.Replace(path, "/");

            // The root path needs no further checking.
            var length = path.Length;
            if (length == 1)
                return path;

            // Base URL paths must end with a slash;
            // non-base URL paths must NOT end with a slash.
            // The final slash is irrelevant for the URL itself
            // (it has to map the same way with or without it)
            // but makes comparing and mapping URLs a lot simpler.
            var finalPosition = length - 1;
            var endsWithSlash = path[finalPosition] == '/';
            return isBasePath
                ? (endsWithSlash ? path : path + "/")
                : (endsWithSlash ? path.Substring(0, finalPosition) : path);
        }

        /// <summary>
        /// Determines whether the specified URL path is prefixed by the specified base URL path.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <param name="basePath">The base URL path.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> is prefixed by <paramref name="basePath"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="path"/> is not a valid URL path.</para>
        /// <para>- or -</para>
        /// <para><paramref name="basePath"/> is not a valid base URL path.</para>
        /// </exception>
        /// <remarks>
        /// <para>This method returns <see langword="true"/> even if the two URL paths are equivalent,
        /// for example if both are <c>"/"</c>, or if <paramref name="path"/> is <c>"/download"</c> and
        /// <paramref name="basePath"/> is <c>"/download/"</c>.</para>
        /// <para>If you are sure that both <paramref name="path"/> and <paramref name="basePath"/>
        /// are valid and normalized, for example because you have called <see cref="Validate.UrlPath"/>,
        /// then you may call <see cref="UnsafeHasPrefix"/> instead of this method. <see cref="UnsafeHasPrefix"/>
        /// is slightly faster because it skips validity checks.</para>
        /// </remarks>
        /// <seealso cref="UnsafeHasPrefix"/>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="StripPrefix"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static bool HasPrefix(string path, string basePath)
            => UnsafeHasPrefix(
                Validate.UrlPath(nameof(path), path, false),
                Validate.UrlPath(nameof(basePath), basePath, true));

        /// <summary>
        /// Determines whether the specified URL path is prefixed by the specified base URL path,
        /// assuming both paths are valid and normalized.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <param name="basePath">The base URL path.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> is prefixed by <paramref name="basePath"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// <para>Unless both <paramref name="path"/> and <paramref name="basePath"/> are valid,
        /// normalized URL paths, the behavior of this method is unspecified. You should call this method
        /// only after calling either <see cref="Normalize"/> or <see cref="Validate.UrlPath"/>
        /// to check and normalize both parameters.</para>
        /// <para>If you are not sure about the validity and/or normalization of parameters,
        /// call <see cref="HasPrefix"/> instead of this method.</para>
        /// <para>This method returns <see langword="true"/> even if the two URL paths are equivalent,
        /// for example if both are <c>"/"</c>, or if <paramref name="path"/> is <c>"/download"</c> and
        /// <paramref name="basePath"/> is <c>"/download/"</c>.</para>
        /// </remarks>
        /// <seealso cref="HasPrefix"/>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="StripPrefix"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static bool UnsafeHasPrefix([ValidatedNotNull] string path, [ValidatedNotNull] string basePath)
            => path.StartsWith(basePath, StringComparison.Ordinal)
            || (path.Length == basePath.Length - 1 && basePath.StartsWith(path, StringComparison.Ordinal));

        /// <summary>
        /// Strips a base URL path fom a URL path, obtaining a relative path.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <param name="basePath">The base URL path.</param>
        /// <returns>The relative path, or <see langword="null"/> if <paramref name="path"/>
        /// is not prefixed by <paramref name="basePath"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="path"/> is not a valid URL path.</para>
        /// <para>- or -</para>
        /// <para><paramref name="basePath"/> is not a valid base URL path.</para>
        /// </exception>
        /// <remarks>
        /// <para>The returned relative path is NOT prefixed by a slash (<c>/</c>) character.</para>
        /// <para>If <paramref name="path"/> and <paramref name="basePath"/> are equivalent,
        /// for example if both are <c>"/"</c>, or if <paramref name="path"/> is <c>"/download"</c>
        /// and <paramref name="basePath"/> is <c>"/download/"</c>, this method returns an empty string.</para>
        /// <para>If you are sure that both <paramref name="path"/> and <paramref name="basePath"/>
        /// are valid and normalized, for example because you have called <see cref="Validate.UrlPath"/>,
        /// then you may call <see cref="UnsafeStripPrefix"/> instead of this method. <see cref="UnsafeStripPrefix"/>
        /// is slightly faster because it skips validity checks.</para>
        /// </remarks>
        /// <seealso cref="UnsafeStripPrefix"/>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="HasPrefix"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static string? StripPrefix(string path, string basePath)
            => UnsafeStripPrefix(
                Validate.UrlPath(nameof(path), path, false),
                Validate.UrlPath(nameof(basePath), basePath, true));

        /// <summary>
        /// Strips a base URL path fom a URL path, obtaining a relative path,
        /// assuming both paths are valid and normalized.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <param name="basePath">The base URL path.</param>
        /// <returns>The relative path, or <see langword="null"/> if <paramref name="path"/>
        /// is not prefixed by <paramref name="basePath"/>.</returns>
        /// <remarks>
        /// <para>Unless both <paramref name="path"/> and <paramref name="basePath"/> are valid,
        /// normalized URL paths, the behavior of this method is unspecified. You should call this method
        /// only after calling either <see cref="Normalize"/> or <see cref="Validate.UrlPath"/>
        /// to check and normalize both parameters.</para>
        /// <para>If you are not sure about the validity and/or normalization of parameters,
        /// call <see cref="StripPrefix"/> instead of this method.</para>
        /// <para>The returned relative path is NOT prefixed by a slash (<c>/</c>) character.</para>
        /// <para>If <paramref name="path"/> and <paramref name="basePath"/> are equivalent,
        /// for example if both are <c>"/"</c>, or if <paramref name="path"/> is <c>"/download"</c>
        /// and <paramref name="basePath"/> is <c>"/download/"</c>, this method returns an empty string.</para>
        /// </remarks>
        /// <seealso cref="StripPrefix"/>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="HasPrefix"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static string? UnsafeStripPrefix(string path, string basePath)
        {
            if (!UnsafeHasPrefix(path, basePath))
                return null;

            // The only case where UnsafeHasPrefix returns true for a urlPath shorter than baseUrlPath
            // is urlPath == (baseUrlPath minus the final slash).
            return path.Length < basePath.Length
                ? string.Empty
                : path.Substring(basePath.Length);
        }

        /// <summary>
        /// Splits the specified URL path into segments.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <returns>An enumeration of path segments.</returns>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not a valid URL path.</exception>
        /// <remarks>
        /// <para>A root URL path (<c>/</c>) will result in an empty enumeration.</para>
        /// <para>The returned enumeration will be the same whether <paramref name="path"/> is a base URL path or not.</para>
        /// <para>If you are sure that <paramref name="path"/> is valid and normalized,
        /// for example because you have called <see cref="Validate.UrlPath"/>,
        /// then you may call <see cref="UnsafeSplit"/> instead of this method. <see cref="UnsafeSplit"/>
        /// is slightly faster because it skips validity checks.</para>
        /// </remarks>
        /// <seealso cref="UnsafeSplit"/>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static IEnumerable<string> Split(string path)
            => UnsafeSplit(Validate.UrlPath(nameof(path), path, false));

        /// <summary>
        /// Splits the specified URL path into segments, assuming it is valid and normalized.
        /// </summary>
        /// <param name="path">The URL path.</param>
        /// <returns>An enumeration of path segments.</returns>
        /// <remarks>
        /// <para>Unless <paramref name="path"/> is a valid, normalized URL path,
        /// the behavior of this method is unspecified. You should call this method
        /// only after calling either <see cref="Normalize"/> or <see cref="Validate.UrlPath"/>
        /// to check and normalize both parameters.</para>
        /// <para>If you are not sure about the validity and/or normalization of <paramref name="path"/>,
        /// call <see cref="StripPrefix"/> instead of this method.</para>
        /// <para>A root URL path (<c>/</c>) will result in an empty enumeration.</para>
        /// <para>The returned enumeration will be the same whether <paramref name="path"/> is a base URL path or not.</para>
        /// </remarks>
        /// <seealso cref="Split"/>
        /// <seealso cref="Normalize"/>
        /// <seealso cref="Validate.UrlPath"/>
        public static IEnumerable<string> UnsafeSplit([ValidatedNotNull] string path)
        {
            var length = path.Length;
            var position = 1; // Skip initial slash
            while (position < length)
            {
                var slashPosition = path.IndexOf('/', position);
                if (slashPosition < 0)
                {
                    yield return path.Substring(position);
                    break;
                }

                yield return path.Substring(position, slashPosition - position);
                position = slashPosition + 1;
            }
        }

        internal static Exception? ValidateInternal(string argumentName, string value)
        {
            if (value == null)
                return new ArgumentNullException(argumentName);

            if (value.Length == 0)
                return new ArgumentException("URL path is empty.", argumentName);

            if (value[0] != '/')
                return new ArgumentException("URL path does not start with a slash.", argumentName);

            return null;
        }
    }
}
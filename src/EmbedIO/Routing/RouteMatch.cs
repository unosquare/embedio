using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EmbedIO.Utilities;

namespace EmbedIO.Routing
{
    /// <summary>
    /// <para>Represents a route resolved by a <see cref="RouteResolverBase{TData}"/>.</para>
    /// <para>This class may be used both as a dictionary of route parameter names and values,
    /// and a list of the values.</para>
    /// <para>Because of its double nature, this class cannot be enumerated directly. However,
    /// you may use the <see cref="Pairs"/> property to iterate over name / value pairs, and the
    /// <see cref="Values"/> property to iterate over values.</para>
    /// <para>When enumerated in a non-generic fashion via the <see cref="IEnumerable"/> interface,
    /// this class iterates over name / value pairs.</para>
    /// </summary>
#pragma warning disable CA1710 // Rename class to end in "Collection"
    public sealed class RouteMatch : IReadOnlyList<string>, IReadOnlyDictionary<string, string>
#pragma warning restore CA1710
    {
        private static readonly IReadOnlyList<string> EmptyStringList = Array.Empty<string>();

        private readonly IReadOnlyList<string> _values;

        internal RouteMatch(string path, IReadOnlyList<string> names, IReadOnlyList<string> values, string? subPath)
        {
            Path = path;
            Names = names;
            _values = values;
            SubPath = subPath;
        }

        /// <summary>
        /// Gets a <see cref="RouteMatch"/> instance that represents no match at all.
        /// </summary>
        /// <remarks>
        /// <para>The <see cref="RouteMatch"/> instance returned by this property
        /// has the following specifications:</para>
        /// <list type="bullet">
        /// <item><description>its <see cref="Path">Path</see> property is the empty string;</description></item>
        /// <item><description>it has no parameters;</description></item>
        /// <item><description>its <see cref="SubPath">SubPath</see> property is <see langword="null"/>.</description></item>
        /// </list>
        /// <para>This <see cref="RouteMatch"/> instance is only useful to initialize
        /// a non-nullable property of type <see cref="RouteMatch"/>, provided that it is subsequently
        /// set to a meaningful value before being used.</para>
        /// </remarks>
        public static RouteMatch None { get; } = new RouteMatch(
            string.Empty,
            Array.Empty<string>(),
            Array.Empty<string>(),
            null);

        /// <summary>
        /// Gets the URL path that was successfully matched against the route.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// <para>For a base route, gets the part of <see cref="Path"/> that follows the matched route;
        /// for a non-base route, this property is always <see langword="null"/>.</para>
        /// </summary>
        public string? SubPath { get; }

        /// <summary>
        /// Gets a list of the names of the route's parameters.
        /// </summary>
        public IReadOnlyList<string> Names { get; }

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count"/>
        public int Count => _values.Count;

        /// <inheritdoc />
        public IEnumerable<string> Keys => Names;

        /// <inheritdoc />
        public IEnumerable<string> Values => _values;

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> interface that can be used
        /// to iterate over name / value pairs.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Pairs => this;

        /// <inheritdoc />
        public string this[int index] => _values[index];

        /// <inheritdoc />
        public string this[string key]
        {
            get
            {
                var count = Names.Count;
                for (var i = 0; i < count; i++)
                {
                    if (Names[i] == key)
                    {
                        return _values[i];
                    }
                }

                throw new KeyNotFoundException("The parameter name was not found.");
            }
        }

        /// <summary>
        /// Returns a <see cref="RouteMatch"/> object equal to the one
        /// that would result by matching the specified URL path against a
        /// base route of <c>"/"</c>.
        /// </summary>
        /// <param name="urlPath">The URL path to match.</param>
        /// <returns>A newly-constructed <see cref="RouteMatch"/>.</returns>
        /// <remarks>
        /// <para>This method assumes that <paramref name="urlPath"/>
        /// is a valid, non-base URL path or route. Otherwise, the behavior of this method
        /// is unspecified.</para>
        /// <para>Ensure that you validate <paramref name="urlPath"/> before
        /// calling this method, using either <see cref="Validate.UrlPath"/>
        /// or <see cref="UrlPath.IsValid"/>.</para>
        /// </remarks>
        public static RouteMatch UnsafeFromRoot(string urlPath)
            => new RouteMatch(urlPath, EmptyStringList, EmptyStringList, urlPath);

        /// <summary>
        /// Returns a <see cref="RouteMatch"/> object equal to the one
        /// that would result by matching the specified URL path against
        /// the specified parameterless base route.
        /// </summary>
        /// <param name="baseUrlPath">The base route to match <paramref name="urlPath"/> against.</param>
        /// <param name="urlPath">The URL path to match.</param>
        /// <returns>A newly-constructed <see cref="RouteMatch"/>.</returns>
        /// <remarks>
        /// <para>This method assumes that <paramref name="baseUrlPath"/> is a
        /// valid base URL path, and <paramref name="urlPath"/>
        /// is a valid, non-base URL path or route. Otherwise, the behavior of this method
        /// is unspecified.</para>
        /// <para>Ensure that you validate both parameters before
        /// calling this method, using either <see cref="Validate.UrlPath"/>
        /// or <see cref="UrlPath.IsValid"/>.</para>
        /// </remarks>
        public static RouteMatch? UnsafeFromBasePath(string baseUrlPath, string urlPath)
        {
            var subPath = UrlPath.UnsafeStripPrefix(urlPath, baseUrlPath);
            return subPath == null ? null : new RouteMatch(urlPath, EmptyStringList, EmptyStringList, "/" + subPath);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key) => Names.Any(n => n == key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out string? value)
        {
            var count = Names.Count;
            for (var i = 0; i < count; i++)
            {
                if (Names[i] == key)
                {
                    value = _values[i];
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Returns the index of the parameter with the specified name.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The index of the parameter, or -1 if none of the
        /// route parameters have the specified name.</returns>
        public int IndexOf(string name)
        {
            var count = Names.Count;
            for (var i = 0; i < count; i++)
            {
                if (Names[i] == name)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <inheritdoc />
        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
            => Names.Zip(_values, (n, v) => new KeyValuePair<string, string>(n, v)).GetEnumerator();

        /// <inheritdoc />
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => _values.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => Pairs.GetEnumerator();
    }
}
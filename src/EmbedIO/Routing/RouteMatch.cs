using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
        private readonly IReadOnlyList<string> _values;

        internal RouteMatch(string path, IReadOnlyList<string> names, IReadOnlyList<string> values, string subPath)
        {
            Path = path;
            Names = names;
            _values = values;
            SubPath = subPath;
        }

        /// <summary>
        /// Gets the URL path that was successfully matched against the route.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// <para>For a base route, gets the part of <see cref="Path"/> that follows the matched route;
        /// for a non-base route, this property is always <see langword="null"/>.</para>
        /// </summary>
        public string SubPath { get; }

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

        /// <inheritdoc />
        public bool ContainsKey(string key) => Names.Any(n => n == key);

        /// <inheritdoc />
        public bool TryGetValue(string key, out string value)
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
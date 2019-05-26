using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Matches URL paths against a route.
    /// </summary>
    public sealed class RouteMatcher
    {
        private static readonly object _syncRoot = new object();
        private static readonly Dictionary<string, RouteMatcher> _cache = new Dictionary<string, RouteMatcher>(StringComparer.Ordinal);

        private readonly Regex _regex;

        private RouteMatcher(string route, string pattern, IReadOnlyList<string> parameterNames)
        {
            Route = route;
            ParameterNames = parameterNames;
            _regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Gets the route this instance matches URL paths against.
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Gets the names of the route's parameters.
        /// </summary>
        public IReadOnlyList<string> ParameterNames { get; }

        /// <summary>
        /// Constructs an instance of <see cref="RouteMatcher"/> by parsing the specified route.
        /// <para>If the same route was previously parsed and the <see cref="ClearCache"/> method has not been called since,
        /// this method obtains an instance from a static cache.</para>
        /// </summary>
        /// <param name="route">The route to parse.</param>
        /// <returns>A newly-constructed instance of <see cref="RouteMatcher"/>
        /// that will match URL paths against <paramref name="route"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="route"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        /// <seealso cref="TryParse"/>
        /// <seealso cref="ClearCache"/>
        public static RouteMatcher Parse(string route)
        {
            var exception = TryParseInternal(route, out var result);
            if (exception != null)
                throw exception;

            return result;
        }

        /// <summary>
        /// <para>Attempts to obtain an instance of <see cref="RouteMatcher" /> by parsing the specified route.</para>
        /// <para>If the same route was previously parsed and the <see cref="ClearCache"/> method has not been called since,
        /// this method obtains an instance from a static cache.</para>
        /// </summary>
        /// <param name="route">The route to parse.</param>
        /// <param name="result">When this method returns <see langword="true"/>, a newly-constructed instance of <see cref="RouteMatcher" />
        /// that will match URL paths against <paramref name="route"/>; otherwise, <see langword="null"/>.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
        /// <seealso cref="Parse"/>
        /// <seealso cref="ClearCache"/>
        public static bool TryParse(string route, out RouteMatcher result)
            => TryParseInternal(route, out result) == null;

        /// <summary>
        /// Clears <see cref="RouteMatcher"/>'s internal instance cache.
        /// </summary>
        /// <seealso cref="Parse"/>
        /// <seealso cref="TryParse"/>
        public static void ClearCache()
        {
            lock (_syncRoot)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Matches the specified URL path against <see cref="Route"/>
        /// and extracts values for the route's parameters.
        /// </summary>
        /// <param name="path">The URL path to match.</param>
        /// <returns>If the match is successful, a <see cref="RouteMatch"/> object;
        /// otherwise, <see langword="null"/>.</returns>
        public RouteMatch Match(string path)
        {
            if (path == null)
                return null;

            var match = _regex.Match(path);

            // Skip the first match group, representing the whole string.
            return match.Success 
                ? new RouteMatch(
                    path,
                    ParameterNames,
                    match.Groups.Cast<Group>().Skip(1).Select(g => g.Value).ToArray())
                : null;
        }

        private static Exception TryParseInternal(string route, out RouteMatcher result)
        {
            lock (_syncRoot)
            {
                if (_cache.TryGetValue(route, out result))
                    return null;

                string pattern = null;
                var parameterNames = new List<string>();
                var exception = Routing.Route.ParseInternal(route, parameterNames.Add, p => pattern = p);
                if (exception != null)
                {
                    result = null;
                    return exception;
                }

                result = new RouteMatcher(route, pattern, parameterNames);
                _cache.Add(route, result);
                return null;
            }
        }
    }
}
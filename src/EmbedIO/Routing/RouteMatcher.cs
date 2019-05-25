using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EmbedIO.Utilities;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Matches URL paths against a route.
    /// </summary>
    public sealed class RouteMatcher
    {
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
        /// </summary>
        /// <param name="route">The route to parse.</param>
        /// <returns>A newly-constructed instance of <see cref="RouteMatcher"/>
        /// that will match URL paths against <paramref name="route"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="route"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RouteMatcher Parse(string route)
        {
            string pattern = null;
            var parameterNames = new List<string>();
            var exception = Routing.Route.ParseInternal(route, parameterNames.Add, p => pattern = p);
            if (exception != null)
                throw exception;

            return new RouteMatcher(route, pattern, parameterNames);
        }

        /// <summary>
        /// Attempts to constructs an instance of <see cref="RouteMatcher" /> by parsing the specified route.
        /// </summary>
        /// <param name="route">The route to parse.</param>
        /// <param name="result">When this method returns <see langword="true"/>, a newly-constructed instance of <see cref="RouteMatcher" />
        /// that will match URL paths against <paramref name="route"/>; otherwise, <see langword="null"/>.
        /// This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string route, out RouteMatcher result)
        {
            string pattern = null;
            var parameterNames = new List<string>();
            var exception = Routing.Route.ParseInternal(route, parameterNames.Add, p => pattern = p);
            if (exception != null)
            {
                result = null;
                return false;
            }

            result = new RouteMatcher(route, pattern, parameterNames);
            return true;
        }

        /// <summary>
        /// Tries to match the specified URL path against <see cref="Route"/>
        /// and extract the route's parameters.
        /// </summary>
        /// <param name="path">The URL path to match.</param>
        /// <returns>If <paramref name="path"/> matches <see cref="Route"/>, a dictionary of parameter names and values;
        /// otherwise, <see langword="null"/>.</returns>
        public IReadOnlyDictionary<string, string> Match(string path)
        {
            if (path == null)
                return null;

            var match = _regex.Match(path);
            if (!match.Success)
                return null;

            var groups = match.Groups;
            var i = 1; // Skip the first match group, representing the whole string.
            return ParameterNames.ToDictionary(n => n, _ => groups[i++].Value);
        }
    }
}
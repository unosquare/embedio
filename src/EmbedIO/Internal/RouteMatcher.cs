using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EmbedIO.Internal
{
    internal sealed class RouteMatcher
    {
        private readonly Regex _regex;

        public RouteMatcher(string route)
        {
            string pattern = null;
            var names = new List<string>();
            var exception = Utilities.Route.ParseInternal(nameof(route), route, names.Add, p => pattern = p);
            if (exception != null)
                throw exception;

            Route = route;
            Parameters = names;
            _regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public string Route { get; }

        public IReadOnlyList<string> Parameters { get; }

        public bool IsMatch(string path, out IReadOnlyList<string> arguments)
        {
            var match = _regex.Match(path);
            if (!match.Success)
            {
                arguments = null;
                return false;
            }

            // The first match group value is the entire string. Skip it.
            var groups = match.Groups;
            var args = new string[groups.Count - 1];
            for (var i = 1; i < groups.Count; i++)
            {
                args[i - 1] = groups[i].Value;
            }

            arguments = args;
            return true;
        }
    }
}
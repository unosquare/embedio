using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace EmbedIO.Internal
{
    internal static class RegexCache
    {
        private const string RegexRouteReplace = "([^//]*)";
        
        private const string WildcardRouteReplace = "(.*)";

        private static readonly ConcurrentDictionary<string, Regex> Cache = new ConcurrentDictionary<string, Regex>();

        private static readonly Regex RouteParamRegex = new Regex(@"\{[^\/]*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        internal static Match MatchRegexStrategy(string url, string input)
        {
            if (!Cache.TryGetValue(url, out var regex))
            {
                regex = new Regex(
                    string.Concat("^", RouteParamRegex.Replace(url, RegexRouteReplace), "$"),
                    RegexOptions.IgnoreCase);

                Cache.TryAdd(url, regex);
            }

            return regex.Match(input);
        }

        internal static Match MatchWildcardStrategy(string url, string input)
        {
            if (!Cache.TryGetValue(url, out var regex))
            {
                regex = new Regex(url.Replace("*", WildcardRouteReplace));

                Cache.TryAdd(url, regex);
            }

            return regex.Match(input);
        }
    }
}

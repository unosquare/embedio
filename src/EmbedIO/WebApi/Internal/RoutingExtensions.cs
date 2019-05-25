using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unosquare.Swan;

namespace EmbedIO.WebApi.Internal
{
    internal static class RoutingExtensions
    {
        private static readonly Regex RouteOptionalParamRegex = new Regex(@"\{[^\/]*\?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="requestPath">The request path.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="validateFunc">The validate function.</param>
        /// <returns>
        /// The params from the request.
        /// </returns>
        public static Dictionary<string, object> RequestRegexUrlParams(
            this string requestPath,
            string basePath,
            Func<bool> validateFunc = null)
        {
            if (validateFunc == null) validateFunc = () => false;
            if (requestPath == basePath && !validateFunc()) return new Dictionary<string, object>();

            var i = 1; // match group index
            var match = RegexCache.MatchRegexStrategy(basePath, requestPath);
            var pathParts = basePath.Split('/');

            if (match.Success && !validateFunc())
            {
                return pathParts
                    .Where(x => x.StartsWith("{"))
                    .ToDictionary(CleanParamId, x => (object)match.Groups[i++].Value);
            }

            var optionalPath = RouteOptionalParamRegex.Replace(basePath, string.Empty);
            var tempPath = requestPath;

            if (optionalPath.Last() == '/' && requestPath.Last() != '/')
            {
                tempPath += "/";
            }

            var subMatch = RegexCache.MatchRegexStrategy(optionalPath, tempPath);

            if (!subMatch.Success || validateFunc()) return null;

            var valuesPaths = optionalPath.Split('/')
                .Where(x => x.StartsWith("{"))
                .ToDictionary(CleanParamId, x => (object)subMatch.Groups[i++].Value);

            var nullPaths = pathParts
                .Where(x => x.StartsWith("{"))
                .Select(CleanParamId);

            foreach (var nullKey in nullPaths)
            {
                if (!valuesPaths.ContainsKey(nullKey))
                    valuesPaths.Add(nullKey, null);
            }

            return valuesPaths;
        }

        internal static string CleanParamId(string val) => val.ReplaceAll(string.Empty, '{', '}', '?');
    }
}
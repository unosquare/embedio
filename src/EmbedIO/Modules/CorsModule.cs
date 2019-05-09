using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO.Modules
{
    /// <summary>
    /// CORS control Module.
    /// Cross-origin resource sharing (CORS) is a mechanism that allows restricted resources (e.g. fonts) 
    /// on a web page to be requested from another domain outside the domain from which the resource originated.
    /// </summary>
    public class CorsModule : WebModuleBase
    {
        private const string Wildcard = "*";

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsModule"/> class.
        /// </summary>
        /// <param name="origins">The origins.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="methods">The methods.</param>
        /// <exception cref="System.ArgumentNullException">
        /// origins
        /// or
        /// headers
        /// or
        /// methods.
        /// </exception>
        public CorsModule(
            string origins = Strings.CorsWildcard, 
            string headers = Strings.CorsWildcard,
            string methods = Strings.CorsWildcard)
        {
            if (origins == null) throw new ArgumentNullException(nameof(origins));
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (methods == null) throw new ArgumentNullException(nameof(methods));
            
            var validOrigins =
                origins.ToLowerInvariant()
                    .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());
            var validMethods =
                methods.ToLowerInvariant()
                    .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (context, ct) =>
            {
                var isOptions = context.RequestVerb() == HttpVerbs.Options;

                // If we allow all we don't need to filter
                if (origins == Strings.CorsWildcard && headers == Strings.CorsWildcard &&
                    methods == Strings.CorsWildcard)
                {
                    context.Response.AddHeader(CorsHeaders.AccessControlAllowOrigin, Wildcard);
                    var result = isOptions && ValidateHttpOptions(methods, context, validMethods);

                    return Task.FromResult(result);
                }

                var currentOrigin = context.RequestHeader(HttpRequestHeaders.Origin);
                
                if (string.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal)
                {
                    return Task.FromResult(false);
                }

                if (origins == Strings.CorsWildcard)
                {
                    return Task.FromResult(false);
                }

                if (validOrigins.Contains(currentOrigin))
                {
                    context.Response.AddHeader(CorsHeaders.AccessControlAllowOrigin,  currentOrigin);

                    if (isOptions)
                    {
                        return Task.FromResult(ValidateHttpOptions(methods, context, validMethods));
                    }
                }

                return Task.FromResult(false);
            });
        }

        private static bool ValidateHttpOptions(
            string methods, 
            IHttpContext context,
            IEnumerable<string> validMethods)
        {
            var currentMethod = context.RequestHeader(CorsHeaders.AccessControlRequestMethod);
            var currentHeader = context.RequestHeader(CorsHeaders.AccessControlRequestHeaders);

            if (!string.IsNullOrWhiteSpace(currentHeader))
            {
                // TODO: I need to remove headers out from AllowHeaders
                context.Response.AddHeader(CorsHeaders.AccessControlAllowHeaders, currentHeader);
            }

            if (string.IsNullOrWhiteSpace(currentMethod)) 
                return true;

            var currentMethods = currentMethod.ToLowerInvariant()
                .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            if (methods == Strings.CorsWildcard || currentMethods.All(validMethods.Contains))
            {
                context.Response.AddHeader(CorsHeaders.AccessControlAllowMethods, currentMethod);

                return true;
            }

            context.Response.StatusCode = (int) System.Net.HttpStatusCode.BadRequest;

            return false;
        }
    }
}
namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System.Threading.Tasks;
    using System;
    using System.Linq;
    using System.Collections.Generic;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    // TODO: Add Whitelist origins with Regex
    // TODO: Add Path Regex, just apply CORS in some paths
    // TODO: Handle valid headers in other modules

    /// <summary>
    /// CORS control Module
    /// Cross-origin resource sharing (CORS) is a mechanism that allows restricted resources (e.g. fonts) 
    /// on a web page to be requested from another domain outside the domain from which the resource originated.
    /// </summary>
    public class CorsModule 
        : WebModuleBase
    {
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
        /// methods
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
                origins.ToLower()
                    .Split(Strings.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());
            var validMethods =
                methods.ToLower()
                    .Split(Strings.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (context, ct) =>
            {
                // If we allow all we don't need to filter
                if (origins == Strings.CorsWildcard && headers == Strings.CorsWildcard &&
                    methods == Strings.CorsWildcard)
                {
                    context.Response.Headers.Add(Headers.AccessControlAllowOrigin);
                    return Task.FromResult(false);
                }

                var currentOrigin = context.RequestHeader(Headers.Origin);
                
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
                    context.Response.Headers.Add(Headers.AccessControlAllowOrigin.Replace("*", currentOrigin));

                    if (context.RequestVerb() == HttpVerbs.Options)
                    {
                        return ValidateHttpOptions(methods, context, validMethods);
                    }
                }

                return Task.FromResult(false);
            });
        }
        
        /// <inheritdoc />
        public override string Name => nameof(CorsModule);

        private static Task<bool> ValidateHttpOptions(
            string methods, 
            HttpListenerContext context,
            IEnumerable<string> validMethods)
        {
            var currentMethod = context.RequestHeader(Headers.AccessControlRequestMethod);
            var currentHeader = context.RequestHeader(Headers.AccessControlRequestHeaders);

            if (!string.IsNullOrWhiteSpace(currentHeader))
            {
                // TODO: I need to remove headers out from AllowHeaders
                context.Response.Headers.Add(Headers.AccessControlAllowHeaders + currentHeader);
            }

            if (!string.IsNullOrWhiteSpace(currentMethod))
            {
                var currentMethods = currentMethod.ToLower()
                    .Split(Strings.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());

                if (methods == Strings.CorsWildcard || currentMethods.All(validMethods.Contains))
                {
                    context.Response.Headers.Add(Headers.AccessControlAllowMethods + currentMethod);
                }
                else
                {
                    context.Response.StatusCode = (int) System.Net.HttpStatusCode.BadRequest;

                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(true);
        }
    }
}
namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System.Threading.Tasks;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    
    /// <summary>
    /// CORS control Module
    /// Cross-origin resource sharing (CORS) is a mechanism that allows restricted resources (e.g. fonts) 
    /// on a web page to be requested from another domain outside the domain from which the resource originated.
    /// </summary>
    public class CorsModule 
        : WebModuleBase
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
                    .Split(Strings.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());
            var validMethods =
                methods.ToLowerInvariant()
                    .Split(Strings.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (context, ct) =>
            {
                var isOptions = context.RequestVerb() == HttpVerbs.Options;

                // If we allow all we don't need to filter
                if (origins == Strings.CorsWildcard && headers == Strings.CorsWildcard &&
                    methods == Strings.CorsWildcard)
                {
                    context.Response.AddHeader(Headers.AccessControlAllowOrigin, Wildcard);
                    var result = isOptions && ValidateHttpOptions(methods, context, validMethods);

                    return Task.FromResult(result);
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
                    context.Response.AddHeader(Headers.AccessControlAllowOrigin,  currentOrigin);

                    if (isOptions)
                    {
                        return Task.FromResult(ValidateHttpOptions(methods, context, validMethods));
                    }
                }

                return Task.FromResult(false);
            });
        }
        
        /// <inheritdoc />
        public override string Name => nameof(CorsModule);

        private static bool ValidateHttpOptions(
            string methods, 
            IHttpContext context,
            IEnumerable<string> validMethods)
        {
            var currentMethod = context.RequestHeader(Headers.AccessControlRequestMethod);
            var currentHeader = context.RequestHeader(Headers.AccessControlRequestHeaders);

            if (!string.IsNullOrWhiteSpace(currentHeader))
            {
                // TODO: I need to remove headers out from AllowHeaders
                context.Response.AddHeader(Headers.AccessControlAllowHeaders, currentHeader);
            }

            if (string.IsNullOrWhiteSpace(currentMethod)) 
                return true;

            var currentMethods = currentMethod.ToLowerInvariant()
                .Split(Strings.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            if (methods == Strings.CorsWildcard || currentMethods.All(validMethods.Contains))
            {
                context.Response.AddHeader(Headers.AccessControlAllowMethods, currentMethod);

                return true;
            }

            context.Response.StatusCode = (int) System.Net.HttpStatusCode.BadRequest;

            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO.Modules
{
    /// <summary>
    /// Cross-origin resource sharing (CORS) control Module.
    /// CORS is a mechanism that allows restricted resources (e.g. fonts) 
    /// on a web page to be requested from another domain outside the domain from which the resource originated.
    /// </summary>
    public class CorsModule : WebModuleBase
    {
        private const string Wildcard = "*";
        private readonly string _origins;
        private readonly string _headers;
        private readonly string _methods;
        private readonly string[] _validOrigins;
        private readonly string[] _validMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="origins">The valid origins, the default value is Wilcard (*).</param>
        /// <param name="headers">The valid headers, the default value is Wilcard (*).</param>
        /// <param name="methods">The valid methods, the default value is Wilcard (*).</param>
        /// <exception cref="ArgumentNullException">
        /// origins
        /// or
        /// headers
        /// or
        /// methods
        /// </exception>
        public CorsModule(
            string baseUrlPath,
            string origins = Strings.CorsWildcard, 
            string headers = Strings.CorsWildcard,
            string methods = Strings.CorsWildcard)
         : base(baseUrlPath)
        {
            _origins = origins ?? throw new ArgumentNullException(nameof(origins));
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
            _methods = methods ?? throw new ArgumentNullException(nameof(methods));
            
            _validOrigins =
                origins.ToLowerInvariant()
                    .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
            _validMethods =
                methods.ToLowerInvariant()
                    .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToArray();
        }

        /// <inheritdoc />
        public override Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct)
        {
            var isOptions = context.RequestVerb() == HttpVerbs.Options;

            // If we allow all we don't need to filter
            if (_origins == Strings.CorsWildcard && _headers == Strings.CorsWildcard &&
                _methods == Strings.CorsWildcard)
            {
                context.Response.AddHeader(HttpHeaders.AccessControlAllowOrigin, Wildcard);
                var result = isOptions && ValidateHttpOptions(_methods, context, _validMethods);

                return Task.FromResult(result);
            }

            var currentOrigin = context.RequestHeader(HttpHeaders.Origin);
                
            if (string.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal)
            {
                return Task.FromResult(false);
            }

            if (_origins == Strings.CorsWildcard)
            {
                return Task.FromResult(false);
            }

            if (_validOrigins.Contains(currentOrigin))
            {
                context.Response.AddHeader(HttpHeaders.AccessControlAllowOrigin,  currentOrigin);

                if (isOptions)
                {
                    return Task.FromResult(ValidateHttpOptions(_methods, context, _validMethods));
                }
            }

            return Task.FromResult(false);
        }
        
        private static bool ValidateHttpOptions(
            string option, 
            IHttpContext context,
            IEnumerable<string> options)
        {
            var currentMethod = context.RequestHeader(HttpHeaders.AccessControlRequestMethod);
            var currentHeader = context.RequestHeader(HttpHeaders.AccessControlRequestHeaders);

            if (!string.IsNullOrWhiteSpace(currentHeader))
            {
                // TODO: I need to remove headers out from AllowHeaders
                context.Response.AddHeader(HttpHeaders.AccessControlAllowHeaders, currentHeader);
            }

            if (string.IsNullOrWhiteSpace(currentMethod)) 
                return true;

            var currentMethods = currentMethod.ToLowerInvariant()
                .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            if (option == Strings.CorsWildcard || currentMethods.All(options.Contains))
            {
                context.Response.AddHeader(HttpHeaders.AccessControlAllowMethods, currentMethod);

                return true;
            }

            context.Response.StatusCode = (int) System.Net.HttpStatusCode.BadRequest;

            return false;
        }
    }
}
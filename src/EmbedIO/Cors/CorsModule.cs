using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;

namespace EmbedIO.Cors
{
    /// <summary>
    /// Cross-origin resource sharing (CORS) control Module.
    /// CORS is a mechanism that allows restricted resources (e.g. fonts) 
    /// on a web page to be requested from another domain outside the domain from which the resource originated.
    /// </summary>
    public class CorsModule : WebModuleBase
    {
        /// <summary>
        /// A string meaning "All" in CORS headers.
        /// </summary>
        public const string All = "*";

        private readonly string _origins;
        private readonly string _headers;
        private readonly string _methods;
        private readonly string[] _validOrigins;
        private readonly string[] _validMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="origins">The valid origins. The default is <see cref="All"/> (<c>*</c>).</param>
        /// <param name="headers">The valid headers. The default is <see cref="All"/> (<c>*</c>).</param>
        /// <param name="methods">The valid methods. The default is <see cref="All"/> (<c>*</c>).</param>
        /// <exception cref="ArgumentNullException">
        /// origins
        /// or
        /// headers
        /// or
        /// methods
        /// </exception>
        public CorsModule(
            string baseUrlPath,
            string origins = All,
            string headers = All,
            string methods = All)
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
        public override Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            var isOptions = context.Request.HttpVerb == HttpVerbs.Options;

            // If we allow all we don't need to filter
            if (_origins == All && _headers == All && _methods == All)
            {
                context.Response.AddHeader(HttpHeaderNames.AccessControlAllowOrigin, All);
                var result = isOptions && ValidateHttpOptions(_methods, context, _validMethods);

                return Task.FromResult(result);
            }

            var currentOrigin = context.RequestHeader(HttpHeaderNames.Origin);
                
            if (String.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal)
            {
                return Task.FromResult(false);
            }

            if (_origins == All)
            {
                return Task.FromResult(false);
            }

            if (_validOrigins.Contains(currentOrigin))
            {
                context.Response.AddHeader(HttpHeaderNames.AccessControlAllowOrigin,  currentOrigin);

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
            var currentMethod = context.RequestHeader(HttpHeaderNames.AccessControlRequestMethod);
            var currentHeader = context.RequestHeader(HttpHeaderNames.AccessControlRequestHeaders);

            if (!String.IsNullOrWhiteSpace(currentHeader))
            {
                // TODO: I need to remove headers out from AllowHeaders
                context.Response.AddHeader(HttpHeaderNames.AccessControlAllowHeaders, currentHeader);
            }

            if (String.IsNullOrWhiteSpace(currentMethod)) 
                return true;

            var currentMethods = currentMethod.ToLowerInvariant()
                .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            if (option == All || currentMethods.All(options.Contains))
            {
                context.Response.AddHeader(HttpHeaderNames.AccessControlAllowMethods, currentMethod);

                return true;
            }

            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;

            return false;
        }
    }
}
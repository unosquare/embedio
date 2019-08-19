using System;
using System.Linq;
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
        /// <param name="baseRoute">The base route.</param>
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
            string baseRoute,
            string origins = All,
            string headers = All,
            string methods = All)
         : base(baseRoute)
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
        public override bool IsFinalHandler => false;

        /// <inheritdoc />
        protected override Task OnRequestAsync(IHttpContext context)
        {
            var isOptions = context.Request.HttpVerb == HttpVerbs.Options;

            // If we allow all we don't need to filter
            if (_origins == All && _headers == All && _methods == All)
            {
                context.Response.Headers.Set(HttpHeaderNames.AccessControlAllowOrigin, All);

                if (isOptions)
                {
                    ValidateHttpOptions(context);
                    context.SetHandled();
                }

                return Task.CompletedTask;
            }

            var currentOrigin = context.Request.Headers[HttpHeaderNames.Origin];

            if (string.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal)
                return Task.CompletedTask;

            if (_origins == All)
                return Task.CompletedTask;

            if (_validOrigins.Contains(currentOrigin))
            {
                context.Response.Headers.Set(HttpHeaderNames.AccessControlAllowOrigin,  currentOrigin);

                if (isOptions)
                {
                    ValidateHttpOptions(context);
                    context.SetHandled();
                }
            }

            return Task.CompletedTask;
        }

        private void ValidateHttpOptions(IHttpContext context)
        {
            var requestHeadersHeader = context.Request.Headers[HttpHeaderNames.AccessControlRequestHeaders];
            if (!string.IsNullOrWhiteSpace(requestHeadersHeader))
            {
                // TODO: Remove unwanted headers from request
                context.Response.Headers.Set(HttpHeaderNames.AccessControlAllowHeaders, requestHeadersHeader);
            }

            var requestMethodHeader = context.Request.Headers[HttpHeaderNames.AccessControlRequestMethod];
            if (string.IsNullOrWhiteSpace(requestMethodHeader)) 
                return;

            var currentMethods = requestMethodHeader.ToLowerInvariant()
                .SplitByComma(StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim());

            if (_methods != All && !currentMethods.Any(_validMethods.Contains))
                throw HttpException.BadRequest();

            context.Response.Headers.Set(HttpHeaderNames.AccessControlAllowMethods, requestMethodHeader);
        }
    }
}
namespace Unosquare.Labs.EmbedIO.Modules
{
    using System.Threading.Tasks;
    using System;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// CORS control Module
    /// Cross-origin resource sharing (CORS) is a mechanism that allows restricted resources (e.g. fonts) 
    /// on a web page to be requested from another domain outside the domain from which the resource originated.
    /// </summary>
    public class CorsModule : WebModuleBase
    {
        /// <summary>
        /// Generates the rules for CORS
        /// 
        /// TODO: Add Whitelist origins with Regex
        /// TODO: Add Path Regex, just apply CORS in some paths
        /// TODO: Handle valid headers in other modules
        /// 
        /// </summary>
        /// <param name="origins">The valid origins, default all</param>
        /// <param name="headers">The valid headers, default all</param>
        /// <param name="methods">The valid method, default all</param>
        public CorsModule(string origins = Constants.CorsWildcard, string headers = Constants.CorsWildcard,
            string methods = Constants.CorsWildcard)
        {

            if (origins == null) throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(origins));
            if (headers == null) throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(headers));
            if (methods == null) throw new ArgumentException(Constants.ArgumentNullExceptionMessage, nameof(methods));

            var validOrigins = origins.ToLower().Split(Constants.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            var validMethods = methods.ToLower().Split(Constants.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (context, ct) =>
            {
                // If we allow all we don't need to filter
                if (origins == Constants.CorsWildcard && headers == Constants.CorsWildcard && methods == Constants.CorsWildcard)
                {
                    context.Response.Headers.Add(Constants.HeaderAccessControlAllowOrigin);
                    return Task.FromResult(false);
                }

                var currentOrigin = context.RequestHeader(Constants.HeaderOrigin);
                var currentHeader = context.RequestHeader(Constants.HeaderAccessControlRequestHeaders);
                var currentMethod = context.RequestHeader(Constants.HeaderAccessControlRequestMethod);

                if (string.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal)
                {
                    return Task.FromResult(false);
                }

                if (origins != Constants.CorsWildcard)
                {
                    if (validOrigins.Contains(currentOrigin))
                    {
                        context.Response.Headers.Add(Constants.HeaderAccessControlAllowOrigin.Replace("*", currentOrigin));

                        if (context.RequestVerb() == HttpVerbs.Options)
                        {
                            if (String.IsNullOrWhiteSpace(currentHeader) == false)
                            {
                                // TODO: I need to remove headers out from AllowHeaders
                                context.Response.Headers.Add(Constants.HeaderAccessControlAllowHeaders + currentHeader);
                            }

                            if (string.IsNullOrWhiteSpace(currentMethod) == false)
                            {
                                var currentMethods = currentMethod.ToLower()
                                    .Split(Constants.CommaSplitChar, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

                                if (methods == Constants.CorsWildcard || currentMethods.All(validMethods.Contains))
                                {
                                    context.Response.Headers.Add(Constants.HeaderAccessControlAllowMethods + currentMethod);
                                }
                                else
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                                    return Task.FromResult(false);
                                }
                            }
                            
                            return Task.FromResult(true);
                        }

                        return Task.FromResult(false);
                    }
                }

                return Task.FromResult(false);
            });
        }

        /// <summary>
        /// Module's name
        /// </summary>
        public override string Name => "CORS Module";
    }
}
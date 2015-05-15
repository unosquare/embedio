namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Linq;

    /// <summary>
    /// CORS control Module
    /// </summary>
    public class CorsModule : WebModuleBase
    {
        private const string CorsWildcard = "*";
        private const string AccessControlAllowOrigin = "Access-Control-Allow-Origin: *";
        private const string AccessControlAllowHeaders = "Access-Control-Allow-Headers: ";
        private const string AccessControlAllowMethods = "Access-Control-Allow-Methods: ";

        /// <summary>
        /// Generates the rules for CORS
        /// </summary>
        /// <param name="origins">The valid origins, default all</param>
        /// <param name="headers">The valid headers, default all</param>
        /// <param name="methods">The valid method, default all</param>
        public CorsModule(string origins = CorsWildcard, string headers = CorsWildcard, string methods = CorsWildcard)
        {
            var validOrigins = origins.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            this.AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (server, context) =>
            {
                // If we allow all we don't need to filter
                if (origins == CorsWildcard && headers == CorsWildcard && methods == CorsWildcard)
                {
                    context.Response.Headers.Add(AccessControlAllowOrigin);
                    return false;
                }

                var currentOrigin = context.RequestHeader("Origin");
                var currentHeader = context.RequestHeader("Access-Control-Request-Headers");
                var currentMethod = context.RequestHeader("Access-Control-Request-Method");

                if (String.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal) return false;

                if (origins != CorsWildcard)
                {
                    if (validOrigins.Contains(currentOrigin))
                    {
                        context.Response.Headers.Add(AccessControlAllowOrigin.Replace("*", currentOrigin));

                        if (context.RequestVerb() == HttpVerbs.Options)
                        {
                            if (String.IsNullOrWhiteSpace(currentHeader) == false)
                                context.Response.Headers.Add(AccessControlAllowHeaders + currentHeader);

                            if (String.IsNullOrWhiteSpace(currentMethod) == false)
                                context.Response.Headers.Add(AccessControlAllowMethods + currentMethod);

                            return true;
                        }

                        return false;
                    }
                }

                // TODO: Implement Methods and Header

                return false;
            });
        }

        public override string Name
        {
            get { return "CORS Module"; }
        }
    }
}
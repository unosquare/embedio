namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Linq;

    /// <summary>
    /// CORS control Module
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

            if (origins == null) throw new ArgumentException("Argument cannot be null.", "origins");
            if (headers == null) throw new ArgumentException("Argument cannot be null.", "headers");
            if (methods == null) throw new ArgumentException("Argument cannot be null.", "methods");

            var validOrigins = origins.ToLower().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            var validHeaders = headers.ToLower().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            var validMethods = methods.ToLower().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, (server, context) =>
            {
                // If we allow all we don't need to filter
                if (origins == Constants.CorsWildcard && headers == Constants.CorsWildcard && methods == Constants.CorsWildcard)
                {
                    context.Response.Headers.Add(Constants.HeaderAccessControlAllowOrigin);
                    return false;
                }

                var currentOrigin = context.RequestHeader(Constants.HeaderOrigin);
                var currentHeader = context.RequestHeader(Constants.HeaderAccessControlRequestHeaders);
                var currentMethod = context.RequestHeader(Constants.HeaderAccessControlRequestMethod);

                if (String.IsNullOrWhiteSpace(currentOrigin) && context.Request.IsLocal) return false;

                if (origins != Constants.CorsWildcard)
                {
                    if (validOrigins.Contains(currentOrigin))
                    {
                        context.Response.Headers.Add(Constants.HeaderAccessControlAllowOrigin.Replace("*", currentOrigin));

                        if (context.RequestVerb() == HttpVerbs.Options)
                        {
                            if (String.IsNullOrWhiteSpace(currentHeader) == false)
                            {
                                var currentHeaders = currentHeader.ToLower()
                                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

                                if (headers == Constants.CorsWildcard || currentHeaders.All(validHeaders.Contains))
                                {
                                    context.Response.Headers.Add(Constants.HeaderAccessControlAllowHeaders + currentHeader);
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            if (String.IsNullOrWhiteSpace(currentMethod) == false)
                            {
                                var currentMethods = currentMethod.ToLower()
                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

                                if (methods == Constants.CorsWildcard || currentMethods.All(validMethods.Contains))
                                {
                                    context.Response.Headers.Add(Constants.HeaderAccessControlAllowMethods + currentMethod);
                                }
                                else
                                {
                                    return false;
                                }
                            }

                            return true;
                        }

                        return false;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Module's name
        /// </summary>
        public override string Name
        {
            get { return "CORS Module"; }
        }
    }
}
namespace EmbedIO.Routing
{
    /// <summary>
    /// Represents the outcome of resolving a context and a path against a route.
    /// </summary>
    public enum RouteResolutionResult
    {
        /* DO NOT reorder members!
         * RouteNotMatched < NoHandlerSelected < NoHandlerSuccessful < Success
         *
         * See comments in RouteResolverBase<,>.ResolveAsync for further explanation.
         */

        /// <summary>
        /// The route didn't match.
        /// </summary>
        RouteNotMatched,

        /// <summary>
        /// The route did match, but no registered handler was suitable for the context.
        /// </summary>
        NoHandlerSelected,

        /// <summary>
        /// The route matched and one or more suitable handlers were found,
        /// but none of them returned <see langword="true"/>.
        /// </summary>
        NoHandlerSuccessful,

        /// <summary>
        /// The route has been resolved.
        /// </summary>
        Success,
    }
}
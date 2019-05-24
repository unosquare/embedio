namespace EmbedIO.Routing
{
    /// <summary>
    /// Represents the outcome of route resolution by a <see cref="RouteResolver"/>.
    /// </summary>
    public enum RouteResult
    {
        /* DO NOT reorder members!
         * RouteNotMatched < MethodNotMatched < OK
         */

        /// <summary>
        /// The route didn't match, ot the handler returned <see langowrd="false"/>.
        /// </summary>
        RouteNotMatched,

        /// <summary>
        /// The route did match, but for a different HTTP method.
        /// </summary>
        MethodNotMatched,

        /// <summary>
        /// The route and HTTP verb were matched, but the route handler
        /// returned <see langword="false"/>.
        /// </summary>
        RouteNotHandled,

        /// <summary>
        /// The route has been resolved.
        /// </summary>
        OK,
    }
}
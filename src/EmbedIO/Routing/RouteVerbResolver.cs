namespace EmbedIO.Routing
{
    /// <summary>
    /// Handles a HTTP request by matching it against a route,
    /// possibly handling different HTTP methods via different handlers.
    /// </summary>
    public sealed class RouteVerbResolver : RouteResolverBase<HttpVerbs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteVerbResolver"/> class.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        public RouteVerbResolver(string route)
            : base(route)
        {
        }

        /// <inheritdoc />
        protected override HttpVerbs GetContextData(IHttpContext context) => context.Request.HttpVerb;

        /// <inheritdoc />
        protected override bool MatchContextData(HttpVerbs contextVerb, HttpVerbs handlerVerb)
            => handlerVerb == HttpVerbs.Any || contextVerb == handlerVerb;
    }
}
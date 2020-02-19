using System;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Handles a HTTP request by matching it against a route,
    /// possibly handling different HTTP methods via different handlers.
    /// </summary>
    public sealed class RouteVerbResolver : RouteResolverBase<HttpVerb>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteVerbResolver"/> class.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> to match URL paths against.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// </exception>
        public RouteVerbResolver(RouteMatcher matcher)
            : base(matcher)
        {
        }

        /// <inheritdoc />
        protected override HttpVerb GetContextData(IHttpContext context) => context.Request.HttpVerb;

        /// <inheritdoc />
        protected override bool MatchContextData(HttpVerb contextData, HttpVerb handlerData)
            => handlerData == HttpVerb.Any || contextData == handlerData;
    }
}
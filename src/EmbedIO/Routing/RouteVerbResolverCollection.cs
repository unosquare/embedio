using Unosquare.Swan;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Handles a HTTP request by matching it against a list of routes,
    /// possibly handling different HTTP methods via different handlers.
    /// </summary>
    /// <seealso cref="RouteResolverBase{TContext,TData}"/>
    /// <seealso cref="RouteVerbResolver"/>
    public sealed class RouteVerbResolverCollection : RouteResolverCollectionBase<IHttpContext, HttpVerbs, RouteVerbResolver>
    {
        private readonly string _logSource;

        internal RouteVerbResolverCollection(string logSource)
        {
            _logSource = logSource;
        }

        /// <inheritdoc />
        protected override RouteVerbResolver CreateResolver(string route) => new RouteVerbResolver(route);

        /// <inheritdoc />
        protected override void OnResolverCalled(IHttpContext context, RouteVerbResolver resolver, RouteResolutionResult result)
            => $"[{context.Id}] Route {resolver.Route} : {result}".Debug(_logSource);
    }
}
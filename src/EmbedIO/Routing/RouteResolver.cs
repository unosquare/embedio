using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO.Routing
{
    public sealed class RouteResolver
    {
        private readonly RouteMatcher _matcher;
        private readonly WebRouteHandler _handler;

        public RouteResolver(HttpVerbs verb, string route, WebRouteHandler handler)
        {
            Verb = verb;
            _matcher = RouteMatcher.Parse(route);
            _handler = Validate.NotNull(nameof(handler), handler);
        }

        public HttpVerbs Verb { get; }

        public string Route => _matcher.Route;

        public async Task<RouteResult> ResolveAsync(IHttpContext context, string path, CancellationToken ct)
        {
            var parameters = _matcher.Match(path);
            if (parameters == null)
                return RouteResult.RouteNotMatched;

            if (Verb != HttpVerbs.Any && Verb != context.Request.HttpVerb)
                return RouteResult.MethodNotMatched;

            return await _handler(context, path, parameters, ct).ConfigureAwait(false)
                ? RouteResult.OK
                : RouteResult.RouteNotHandled;
        }
    }
}
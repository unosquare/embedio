using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Base class for modules that handle requests by resolving route / method pairs associated with handlers.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public abstract class RoutingModuleBase : WebModuleBase
    {
        private readonly RouteVerbResolverCollection _resolvers = new RouteVerbResolverCollection(nameof(RoutingModuleBase));

        /// <inheritdoc cref="WebModuleBase(string)"/>
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingModuleBase"/> class.
        /// </summary>
        protected RoutingModuleBase(string baseUrlPath)
            : base(baseUrlPath)
        {
        }

        /// <inheritdoc />
        public override async Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            var result = await _resolvers.ResolveAsync(context, path, cancellationToken).ConfigureAwait(false);
            switch (result)
            {
                case RouteResolutionResult.RouteNotMatched:
                case RouteResolutionResult.NoHandlerSuccessful:
                    return await OnPathNotFoundAsync(context, path, cancellationToken).ConfigureAwait(false);
                case RouteResolutionResult.NoHandlerSelected:
                    return await OnMethodNotAllowedAsync(context, path, cancellationToken).ConfigureAwait(false);
                case RouteResolutionResult.Success:
                    return true;
                default:
                    throw new Exception($"Internal error: unknown route resolution result {result}.");
            }
        }

        /// <summary>
        /// Associates a HTTP method and a route to a handler.
        /// </summary>
        /// <param name="verb">A <see cref="HttpVerbs"/> constant representing the HTTP method
        /// to associate with <paramref name="handler"/>, or <see cref="HttpVerbs.Any"/>
        /// if <paramref name="handler"/> can handle all HTTP methods.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void AddHandler(HttpVerbs verb, string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(verb, route, handler);

        /// <summary>
        /// Associates a HTTP method and a route to a synchronous handler.
        /// </summary>
        /// <param name="verb">A <see cref="HttpVerbs"/> constant representing the HTTP method
        /// to associate with <paramref name="handler"/>, or <see cref="HttpVerbs.Any"/>
        /// if <paramref name="handler"/> can handle all HTTP methods.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void AddHandler(HttpVerbs verb, string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(verb, route, handler);

        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteHandlerAttribute">RouteHandler</see> attributes.</para>
        /// <para>See <see cref="RouteVerbResolverCollection.AddFrom(object)"/> for further information.</para>
        /// </summary>
        /// <param name="target">Where to look for compatible handlers.</param>
        /// <returns>The number of handlers that were added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        protected int AddHandlersFrom(object target)
            => _resolvers.AddFrom(target);

        /// <summary>
        /// Associates all requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnAny(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Any, route, handler);

        /// <summary>
        /// Associates all requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnAny(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Any, route, handler);

        /// <summary>
        /// Associates <c>DELETE</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnDelete(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Delete, route, handler);

        /// <summary>
        /// Associates <c>DELETE</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnDelete(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Delete, route, handler);

        /// <summary>
        /// Associates <c>GET</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnGet(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Get, route, handler);

        /// <summary>
        /// Associates <c>GET</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnGet(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Get, route, handler);

        /// <summary>
        /// Associates <c>HEAD</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnHead(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Head, route, handler);

        /// <summary>
        /// Associates <c>HEAD</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnHead(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Head, route, handler);

        /// <summary>
        /// Associates <c>OPTIONS</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnOptions(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Options, route, handler);

        /// <summary>
        /// Associates <c>OPTIONS</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnOptions(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Options, route, handler);

        /// <summary>
        /// Associates <c>PATCH</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnPatch(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Patch, route, handler);

        /// <summary>
        /// Associates <c>PATCH</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnPatch(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Patch, route, handler);

        /// <summary>
        /// Associates <c>POST</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnPost(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Post, route, handler);

        /// <summary>
        /// Associates <c>POST</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnPost(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Post, route, handler);

        /// <summary>
        /// Associates <c>PUT</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnPut(string route, RouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Put, route, handler);

        /// <summary>
        /// Associates <c>PUT</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnPut(string route, SyncRouteHandler<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Put, route, handler);

        /// <summary>
        /// <para>Called when no route is matched for the requested URL path.</para>
        /// <para>The default behavior is to send an empty <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        protected virtual Task<bool> OnPathNotFoundAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            context.Response.StandardResponseWithoutBody((int)HttpStatusCode.NotFound);
            return Task.FromResult(true);
        }

        /// <summary>
        /// <para>Called when at least one route is matched for the requested URL path,
        /// but none of them is associated with the HTTP method of the request.</para>
        /// <para>The default behavior is to send an empty <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// <see langword="true" /> if the request has been handled;
        /// <see langword="false" /> if the request should be passed down the module chain.
        /// </returns>
        protected virtual Task<bool> OnMethodNotAllowedAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            context.Response.StandardResponseWithoutBody((int)HttpStatusCode.MethodNotAllowed);
            return Task.FromResult(true);
        }
    }
}
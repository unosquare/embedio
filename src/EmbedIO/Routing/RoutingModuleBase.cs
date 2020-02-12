using System;
using System.Threading.Tasks;
using Swan;

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
        protected RoutingModuleBase(string baseRoute)
            : base(baseRoute)
        {
        }

        /// <inheritdoc />
        public override bool IsFinalHandler => true;

        /// <inheritdoc />
        protected override async Task OnRequestAsync(IHttpContext context)
        {
            var result = await _resolvers.ResolveAsync(context).ConfigureAwait(false);
            switch (result)
            {
                case RouteResolutionResult.RouteNotMatched:
                case RouteResolutionResult.NoHandlerSuccessful:
                    await OnPathNotFoundAsync(context).ConfigureAwait(false);
                    break;
                case RouteResolutionResult.NoHandlerSelected:
                    await OnMethodNotAllowedAsync(context).ConfigureAwait(false);
                    break;
                case RouteResolutionResult.Success:
                    return;
                default:
                    throw SelfCheck.Failure($"Internal error: unknown route resolution result {result}.");
            }
        }

        /// <summary>
        /// Associates a HTTP method and a route to a handler.
        /// </summary>
        /// <param name="verb">A <see cref="HttpVerbs"/> constant representing the HTTP method
        /// to associate with <paramref name="handler"/>, or <see cref="HttpVerbs.Any"/>
        /// if <paramref name="handler"/> can handle all HTTP methods.</param>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void AddHandler(HttpVerbs verb, RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(verb, matcher, handler);

        /// <summary>
        /// Associates a HTTP method and a route to a synchronous handler.
        /// </summary>
        /// <param name="verb">A <see cref="HttpVerbs"/> constant representing the HTTP method
        /// to associate with <paramref name="handler"/>, or <see cref="HttpVerbs.Any"/>
        /// if <paramref name="handler"/> can handle all HTTP methods.</param>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void AddHandler(HttpVerbs verb, RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(verb, matcher, handler);

        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteAttribute">Route</see> attributes.</para>
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
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnAny(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Any, matcher, handler);

        /// <summary>
        /// Associates all requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnAny(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Any, matcher, handler);

        /// <summary>
        /// Associates <c>DELETE</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnDelete(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Delete, matcher, handler);

        /// <summary>
        /// Associates <c>DELETE</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnDelete(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Delete, matcher, handler);

        /// <summary>
        /// Associates <c>GET</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnGet(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Get, matcher, handler);

        /// <summary>
        /// Associates <c>GET</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnGet(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Get, matcher, handler);

        /// <summary>
        /// Associates <c>HEAD</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnHead(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Head, matcher, handler);

        /// <summary>
        /// Associates <c>HEAD</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnHead(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Head, matcher, handler);

        /// <summary>
        /// Associates <c>OPTIONS</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnOptions(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Options, matcher, handler);

        /// <summary>
        /// Associates <c>OPTIONS</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnOptions(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Options, matcher, handler);

        /// <summary>
        /// Associates <c>PATCH</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnPatch(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Patch, matcher, handler);

        /// <summary>
        /// Associates <c>PATCH</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnPatch(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Patch, matcher, handler);

        /// <summary>
        /// Associates <c>POST</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnPost(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Post, matcher, handler);

        /// <summary>
        /// Associates <c>POST</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnPost(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Post, matcher, handler);

        /// <summary>
        /// Associates <c>PUT</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnPut(RouteMatcher matcher, RouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Put, matcher, handler);

        /// <summary>
        /// Associates <c>PUT</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="matcher">The <see cref="RouteMatcher"/> used to match URL paths.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="matcher"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        protected void OnPut(RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => _resolvers.Add(HttpVerbs.Put, matcher, handler);

        /// <summary>
        /// <para>Called when no route is matched for the requested URL path.</para>
        /// <para>The default behavior is to send an empty <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        protected virtual Task OnPathNotFoundAsync(IHttpContext context)
            => throw HttpException.NotFound();

        /// <summary>
        /// <para>Called when at least one route is matched for the requested URL path,
        /// but none of them is associated with the HTTP method of the request.</para>
        /// <para>The default behavior is to send an empty <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        protected virtual Task OnMethodNotAllowedAsync(IHttpContext context)
            => throw HttpException.MethodNotAllowed();
    }
}
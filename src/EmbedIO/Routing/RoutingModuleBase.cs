using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;

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
        public override bool IsFinalHandler => true;

        /// <inheritdoc />
        protected override async Task OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            var result = await _resolvers.ResolveAsync(context, path, cancellationToken).ConfigureAwait(false);
            switch (result)
            {
                case RouteResolutionResult.RouteNotMatched:
                case RouteResolutionResult.NoHandlerSuccessful:
                    await OnPathNotFoundAsync(context, path, cancellationToken).ConfigureAwait(false);
                    break;
                case RouteResolutionResult.NoHandlerSelected:
                    await OnMethodNotAllowedAsync(context, path, cancellationToken).ConfigureAwait(false);
                    break;
                case RouteResolutionResult.Success:
                    return;
                default:
                    SelfCheck.Fail($"Internal error: unknown route resolution result {result}.");
                    return;
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
        protected void AddHandler(HttpVerbs verb, string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void AddHandler(HttpVerbs verb, string route, SyncRouteHandlerCallback<IHttpContext> handler)
            => _resolvers.Add(verb, route, handler);

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
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        protected void OnAny(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnAny(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnDelete(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnDelete(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnGet(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnGet(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnHead(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnHead(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnOptions(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnOptions(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnPatch(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnPatch(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnPost(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnPost(string route, SyncRouteHandlerCallback<IHttpContext> handler)
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
        protected void OnPut(string route, RouteHandlerCallback<IHttpContext> handler)
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
        protected void OnPut(string route, SyncRouteHandlerCallback<IHttpContext> handler)
            => _resolvers.Add(HttpVerbs.Put, route, handler);

        /// <summary>
        /// <para>Called when no route is matched for the requested URL path.</para>
        /// <para>The default behavior is to send an empty <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        protected virtual Task OnPathNotFoundAsync(IHttpContext context, string path, CancellationToken cancellationToken)
            => throw HttpException.NotFound();

        /// <summary>
        /// <para>Called when at least one route is matched for the requested URL path,
        /// but none of them is associated with the HTTP method of the request.</para>
        /// <para>The default behavior is to send an empty <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        protected virtual Task OnMethodNotAllowedAsync(IHttpContext context, string path, CancellationToken cancellationToken)
            => throw HttpException.MethodNotAllowed();
    }
}
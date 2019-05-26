using System;

namespace EmbedIO.Routing
{
    /// <summary>
    /// A module that handles requests by resolving route / method pairs associated with handlers.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class RoutingModule : RoutingModuleBase
    {
        /// <inheritdoc cref="WebModuleBase(string)"/>
        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingModule"/> class.
        /// </summary>
        public RoutingModule(string baseUrlPath)
            : base(baseUrlPath)
        {
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
        public void Add(HttpVerbs verb, string route, RouteHandler<IHttpContext> handler)
            => AddHandler(verb, route, handler);

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
        public void Add(HttpVerbs verb, string route, SyncRouteHandler<IHttpContext> handler)
            => AddHandler(verb, route, handler);

        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteHandlerAttribute">RouteHandler</see> attributes.</para>
        /// <para>See <see cref="RouteVerbResolverCollection.AddFrom(object)"/> for further information.</para>
        /// </summary>
        /// <param name="target">Where to look for compatible handlers.</param>
        /// <returns>The number of handlers that were added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        public int AddFrom(object target) => AddHandlersFrom(target);
    }
}
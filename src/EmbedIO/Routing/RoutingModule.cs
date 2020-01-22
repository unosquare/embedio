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
        public RoutingModule(string baseRoute)
            : base(baseRoute)
        {
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
        public void Add(HttpVerbs verb, RouteMatcher matcher, RouteHandlerCallback handler)
            => AddHandler(verb, matcher, handler);

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
        public void Add(HttpVerbs verb, RouteMatcher matcher, SyncRouteHandlerCallback handler)
            => AddHandler(verb, matcher, handler);

        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteAttribute">Route</see> attributes.</para>
        /// <para>See <see cref="RouteVerbResolverCollection.AddFrom(object)"/> for further information.</para>
        /// </summary>
        /// <param name="target">Where to look for compatible handlers.</param>
        /// <returns>The number of handlers that were added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        public int AddFrom(object target) => AddHandlersFrom(target);
    }
}
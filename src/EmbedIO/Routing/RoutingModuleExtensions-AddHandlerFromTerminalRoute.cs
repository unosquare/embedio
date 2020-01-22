using System;

namespace EmbedIO.Routing
{
    partial class RoutingModuleExtensions
    {
        /// <summary>
        /// Adds a handler to a <see cref="RoutingModule"/>.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="verb">A <see cref="HttpVerbs"/> constant representing the HTTP method
        /// to associate with <paramref name="handler"/>, or <see cref="HttpVerbs.Any"/>
        /// if <paramref name="handler"/> can handle all HTTP methods.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        /// <seealso cref="RoutingModule.Add(HttpVerbs,RouteMatcher,RouteHandlerCallback)"/>
        public static RoutingModule Handle(this RoutingModule @this, HttpVerbs verb, string route, RouteHandlerCallback handler)
        {
            @this.Add(verb, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Adds a synchronous handler to a <see cref="RoutingModule"/>.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="verb">A <see cref="HttpVerbs"/> constant representing the HTTP method
        /// to associate with <paramref name="handler"/>, or <see cref="HttpVerbs.Any"/>
        /// if <paramref name="handler"/> can handle all HTTP methods.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        /// <seealso cref="RoutingModule.Add(HttpVerbs,RouteMatcher,RouteHandlerCallback)"/>
        public static RoutingModule Handle(this RoutingModule @this, HttpVerbs verb, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(verb, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates all requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnAny(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Any, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates all requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnAny(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Any, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>DELETE</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnDelete(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Delete, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>DELETE</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnDelete(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Delete, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>GET</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnGet(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Get, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>GET</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnGet(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Get, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>HEAD</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnHead(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Head, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>HEAD</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnHead(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Head, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>OPTIONS</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnOptions(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Options, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>OPTIONS</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnOptions(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Options, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>PATCH</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnPatch(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Patch, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>PATCH</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnPatch(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Patch, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>POST</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnPost(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Post, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>POST</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnPost(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Post, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>PUT</c> requests matching a route to a handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnPut(this RoutingModule @this, string route, RouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Put, RouteMatcher.Parse(route, false), handler);
            return @this;
        }

        /// <summary>
        /// Associates <c>PUT</c> requests matching a route to a synchronous handler.
        /// </summary>
        /// <param name="this">The <see cref="RoutingModule"/> on which this method is called.</param>
        /// <param name="route">The route to match URL paths against.</param>
        /// <param name="handler">A callback used to handle matching contexts.</param>
        /// <returns><paramref name="this"/> with the handler added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="route"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="handler"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="route"/> is not a valid route.</exception>
        public static RoutingModule OnPut(this RoutingModule @this, string route, SyncRouteHandlerCallback handler)
        {
            @this.Add(HttpVerbs.Put, RouteMatcher.Parse(route, false), handler);
            return @this;
        }
    }
}
using System;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteAttribute"/> class.
        /// </summary>
        /// <param name="isBaseRoute"><see langword="true"/> if this attribute represents a base route;
        /// <see langword="false"/> (the default) if it represents a terminal (non-base) route.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="route">The route.</param>
        /// <exception cref="ArgumentNullException"><paramref name="route"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="route"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="route"/> does not start with a slash (<c>/</c>) character.</para>
        /// <para>- or -</para>
        /// <para><paramref name="route"/> does not comply with route syntax.</para>
        /// </exception>
        /// <seealso cref="Routing.Route.IsValid"/>
        public RouteAttribute(HttpVerbs verb, string route, bool isBaseRoute = false)
        {
            Matcher = RouteMatcher.Parse(route, isBaseRoute);
            Verb = verb;
        }

        /// <summary>
        /// Gets the HTTP verb handled by a method with this attribute.
        /// </summary>
        public HttpVerbs Verb { get; }

        /// <summary>
        /// Gets a <see cref="RouteMatcher"/> that will match URLs against this attribute's data.
        /// </summary>
        public RouteMatcher Matcher { get; }

        /// <summary>
        /// Gets the route handled by a method with this attribute.
        /// </summary>
        public string Route => Matcher.Route;

        /// <summary>
        /// Gets a value indicating whether this attribute represents a base route.
        /// </summary>
        public bool IsBaseRoute => Matcher.IsBaseRoute;
    }
}
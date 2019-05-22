using System;
using EmbedIO.Constants;
using EmbedIO.Utilities;

namespace EmbedIO.Modules
{
    /// <summary>
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class WebApiHandlerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiHandlerAttribute"/> class.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="route">The route.</param>
        /// <exception cref="ArgumentNullException"><paramref name="route"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="route"/> is empty.</para>
        /// <para>- or -</para>
        /// <para><paramref name="route"/> does not start with a slash (<c>/</c>) character.</para>
        /// </exception>
        public WebApiHandlerAttribute(HttpVerbs verb, string route)
        {
            Verb = verb;
            Route = Validate.UrlPath(nameof(route), route, false);
        }

        /// <summary>
        /// Gets the HTTP verb handled by a method with this attribute.
        /// </summary>
        public HttpVerbs Verb { get; }

        /// <summary>
        /// Gets the route handled by a method with this attribute.
        /// </summary>
        public string Route { get; }
    }
}
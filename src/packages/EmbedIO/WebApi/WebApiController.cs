using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using EmbedIO.Routing;
using EmbedIO.Sessions;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// Inherit from this class and define your own Web API methods
    /// You must RegisterController in the Web API Module to make it active.
    /// </summary>
    public abstract class WebApiController
    {
// The HttpContext and Route properties are always initialized to non-null values
// by the same runtime-compiled lambda that creates the controller.
// Unfortunately, code analyzers cannot know about the lambda; hence the warnings.
#pragma warning disable CS8618 // Non-nullable property is uninitialized. Consider declaring the property as nullable.

        /// <summary>
        /// <para>Gets the HTTP context.</para>
        /// <para>This property is automatically initialized upon controller creation.</para>
        /// </summary>
        public IHttpContext HttpContext { get; internal set; }

        /// <summary>
        /// <para>Gets the resolved route.</para>
        /// <para>This property is automatically initialized upon controller creation.</para>
        /// </summary>
        public RouteMatch Route { get; internal set; }

#pragma warning restore CS8618

        /// <summary>
        /// Gets the <see cref="CancellationToken" /> used to cancel processing of the request.
        /// </summary>
        public CancellationToken CancellationToken => HttpContext.CancellationToken;

        /// <summary>
        /// Gets the HTTP request.
        /// </summary>
        public IHttpRequest Request => HttpContext.Request;

        /// <summary>
        /// Gets the HTTP response object.
        /// </summary>
        public IHttpResponse Response => HttpContext.Response;

        /// <summary>
        /// Gets the user.
        /// </summary>
        public IPrincipal? User => HttpContext.User;

        /// <summary>
        /// Gets the session proxy associated with the HTTP context.
        /// </summary>
        public ISessionProxy Session => HttpContext.Session;

        /// <summary>
        /// Gets the method of the controller that will be called by the current route.
        /// </summary>
        internal MethodInfo CurrentMethod => GetType().GetMethods()
            .FirstOrDefault(m => m.GetCustomAttributes<RouteAttribute>(true)
                .Any(ca => ca.Verb == HttpContext.Request.HttpVerb && ca.Matcher.Match(HttpContext.Route.SubPath) != null));

        /// <summary>
        /// <para>This method is meant to be called internally by EmbedIO.</para>
        /// <para>Derived classes can override the <see cref="OnBeforeHandler"/> method
        /// to perform common operations before any handler gets called.</para>
        /// </summary>
        /// <seealso cref="OnBeforeHandler"/>
        public void PreProcessRequest() => OnBeforeHandler();

        /// <summary>
        /// <para>Called before a handler to perform common operations.</para>
        /// <para>The default behavior is to set response headers
        /// in order to prevent caching of the response.</para>
        /// </summary>
        protected virtual void OnBeforeHandler() => HttpContext.Response.DisableCaching();
    }
}
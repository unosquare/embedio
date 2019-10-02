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
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiController" /> class.
        /// </summary>
        protected WebApiController()
        {
        }

        /// <summary>
        /// <para>Gets the HTTP context.</para>
        /// <para>This property is automatically initialized upon controller creation.</para>
        /// </summary>
        public IHttpContext HttpContext { get; internal set; }

        /// <summary>
        /// <para>Gets the resolved route.</para>
        /// <para>This property is automatically initialized upon controller creation.</para>
        /// </summary>
        public RouteMatch? Route { get; internal set; }

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
        public IPrincipal User => HttpContext.User;

        /// <summary>
        /// Gets the session proxy associated with the HTTP context.
        /// </summary>
        public ISessionProxy Session => HttpContext.Session;

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
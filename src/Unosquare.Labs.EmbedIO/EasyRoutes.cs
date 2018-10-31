namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Threading;
    using Modules;

    /// <summary>
    /// Extension methods to add easily routes to a <c>IWebServer</c>.
    /// </summary>
    public static class EasyRoutes
    {
        /// <summary>
        /// Called when any unhandled request.
        /// Any verb and any path.
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnAny(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Any);

        /// <summary>
        /// Called when any POST unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnPost(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Post);
        
        /// <summary>
        /// Called when any GET unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnGet(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Get);
        
        /// <summary>
        /// Called when any PUT unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnPut(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Put);
        
        /// <summary>
        /// Called when any DELETE unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnDelete(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Delete);
        
        /// <summary>
        /// Called when any HEAD unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnHead(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Head);
        
        /// <summary>
        /// Called when any OPTIONS unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnOptions(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Options);
        
        /// <summary>
        /// Called when any PATCH unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnPatch(this IWebServer webserver, WebModuleBase.WebHandler action)
            => AddFallbackModule(webserver, action, Constants.HttpVerbs.Patch);

        private static IWebServer AddFallbackModule(IWebServer webserver, WebModuleBase.WebHandler action, Constants.HttpVerbs verb)
        {
            if (webserver == null)
                throw new ArgumentNullException(nameof(webserver));

            webserver.RegisterModule(new FallbackModule(action, verb));

            return webserver;
        }
    }
}

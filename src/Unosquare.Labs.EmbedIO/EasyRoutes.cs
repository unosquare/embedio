namespace Unosquare.Labs.EmbedIO
{
    using System;

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
        public static IWebServer OnAny(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Any, action);

        /// <summary>
        /// Called when any POST unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnPost(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Post, action);
        
        /// <summary>
        /// Called when any GET unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnGet(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Get, action);
        
        /// <summary>
        /// Called when any PUT unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnPut(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Put, action);
        
        /// <summary>
        /// Called when any DELETE unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnDelete(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Delete, action);
        
        /// <summary>
        /// Called when any HEAD unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnHead(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Head, action);
        
        /// <summary>
        /// Called when any OPTIONS unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnOptions(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Options, action);

        /// <summary>
        /// Called when any PATCH unhandled request (any path).
        /// </summary>
        /// <param name="webserver">The webserver.</param>
        /// <param name="action">The action.</param>
        /// <returns>
        /// The webserver instance.
        /// </returns>
        /// <exception cref="ArgumentNullException">webserver.</exception>
        public static IWebServer OnPatch(this IWebServer webserver, WebHandler action)
            => webserver.WithAction(ModuleMap.AnyPath, Constants.HttpVerbs.Patch, action);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Modules
{
    /// <summary>
    /// A very simple module to register class methods as handlers.
    /// Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests.
    /// </summary>
    public class WebApiModule : WebModuleBase
    {
        private readonly HashSet<Type> _controllerTypes = new HashSet<Type>();

        private readonly List<Func<IHttpContext, CancellationToken, WebApiController>> _controllerFactories
            = new List<Func<IHttpContext, CancellationToken, WebApiController>>();

        private readonly Dictionary<string, Dictionary<HttpVerbs, MethodCacheInstance>> _delegateMap
            = new Dictionary<string, Dictionary<HttpVerbs, MethodCacheInstance>>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiModule"/> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path served by this module.</param>
        /// <param name="routingStrategy">The routing strategy employed to select a controller.</param>
        /// <param name="sendJsonOnException"><see langword="true"/> to include a JSON description
        /// of exceptions thrown by controllers in <c>500 Internal Server Error</c> responses.</param>
        /// <seealso cref="IWebModule.BaseUrlPath" />
        /// <seealso cref="Validate.UrlPath" />
        /// <seealso cref="RoutingStrategy"/>
        /// <seealso cref="SendJsonOnException"/>
        public WebApiModule(
            string baseUrlPath,
            WebApiRoutingStrategy routingStrategy = WebApiRoutingStrategy.Regex,
            bool sendJsonOnException = false)
            : base(baseUrlPath)
        {
            RoutingStrategy = Validate.EnumValue<WebApiRoutingStrategy>(nameof(routingStrategy), routingStrategy);
            SendJsonOnException = sendJsonOnException;
        }

        /// <summary>
        /// Gets the routing strategy used by this module
        /// to select a controller based on the requested URL path.
        /// </summary>
        protected WebApiRoutingStrategy RoutingStrategy { get; }

        /// <summary>
        /// <para>Gets a value indicating whether a JSON description
        /// of exceptions thrown by controllers is included in
        /// <c>500 Internal Server Error</c> responses.</para>
        /// <para>Note that derived classes overriding the
        /// <see cref="OnExceptionAsync"/> method may choose to ignore
        /// the value of this property.</para>
        /// </summary>
        protected bool SendJsonOnException { get; }

        /// <summary>
        /// Gets the number of controller objects registered in this API.
        /// </summary>
        public int ControllersCount => _controllerTypes.Count;

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <exception cref="ArgumentException">Controller types must be unique within the module.</exception>
        public void RegisterController<TController>()
            where TController : WebApiController
            => RegisterController(typeof(TController));

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <param name="factory">The controller factory method.</param>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module.</exception>
        public void RegisterController<TController>(Func<IHttpContext, CancellationToken, TController> factory)
            where TController : WebApiController
            => RegisterController(typeof(TController), factory);

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <param name="controllerType">Tht type of the controller.</param>
        public void RegisterController(Type controllerType)
            => RegisterController(controllerType, (ctx, ct) => Activator.CreateInstance(controllerType, ctx) as WebApiController);

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <param name="factory">The factory.</param>
        /// <exception cref="ArgumentException">Controller types must be unique within the module</exception>
        public void RegisterController(Type controllerType, Func<IHttpContext, CancellationToken, WebApiController> factory)
        {
            if (_controllerTypes.Contains(controllerType))
                throw new ArgumentException("Controller types must be unique within the module");

            var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.ReturnType == typeof(bool)
                          || m.ReturnType == typeof(Task<bool>));

            foreach (var method in methods)
            {
                if (!(method.GetCustomAttributes(typeof(WebApiHandlerAttribute), true).FirstOrDefault() is WebApiHandlerAttribute attribute))
                    continue;

                foreach (var path in attribute.Paths)
                {
                    if (_delegateMap.ContainsKey(path) == false)
                    {
                        _delegateMap.Add(path, new Dictionary<HttpVerbs, MethodCacheInstance>()); // add
                    }

                    var delegatePair = new MethodCacheInstance(factory, new MethodCache(method));

                    if (_delegateMap[path].ContainsKey(attribute.Verb))
                        _delegateMap[path][attribute.Verb] = delegatePair; // update
                    else
                        _delegateMap[path].Add(attribute.Verb, delegatePair); // add
                }
            }

            _controllerTypes.Add(controllerType);
        }

        /// <summary>
        /// Normalizes a path meant for Regex matching, extracts the route parameters, and returns the registered
        /// path in the internal delegate map.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="context">The context.</param>
        /// <param name="routeParams">The route parameters.</param>
        /// <returns>A string that represents the registered path in the internal delegate map.</returns>
        private string NormalizeRegexPath(
            HttpVerbs verb,
            IHttpContext context,
            IDictionary<string, object> routeParams)
        {
            var path = context.Request.Url.AbsolutePath;

            foreach (var route in _delegateMap.Keys)
            {
                var urlParam = path.RequestRegexUrlParams(route, () => !_delegateMap[route].Keys.Contains(verb));

                if (urlParam == null) continue;

                foreach (var kvp in urlParam)
                {
                    routeParams.Add(kvp.Key, kvp.Value);
                }

                return route;
            }

            return null;
        }

        /// <summary>
        /// Normalizes a URL request path meant for Wildcard matching and returns the registered
        /// path in the internal delegate map.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="context">The context.</param>
        /// <returns>A string that represents the registered path.</returns>
        private string NormalizeWildcardPath(HttpVerbs verb, IHttpContext context, string path)
        {
            if (_delegateMap.ContainsKey(path) == false)
                return null;

            if (_delegateMap[path].ContainsKey(verb))
                return path;

            var originalPath = context.RequestPath();

            if (_delegateMap.ContainsKey(originalPath) &&
                _delegateMap[originalPath].ContainsKey(verb))
            {
                return originalPath;
            }

            return null;
        }

        /// <summary>
        /// Looks for a path that matches the one provided by the context.
        /// </summary>
        /// <param name="context"> The HttpListener context.</param>
        /// <returns><c>true</c> if the path is found, otherwise <c>false</c>.</returns>
        private bool IsMethodNotAllowed(IHttpContext context)
        {
            string path;

            switch (RoutingStrategy)
            {
                case WebApiRoutingStrategy.Wildcard:
                    path = context.RequestWilcardPath(_delegateMap.Keys
                        .Where(k => k.Contains(ModuleMap.AnyPathRoute))
                        .Select(s => s.ToLowerInvariant()));
                    break;
                case WebApiRoutingStrategy.Regex:
                    path = context.Request.Url.AbsolutePath;
                    foreach (var route in _delegateMap.Keys)
                    {
                        if (path.RequestRegexUrlParams(route) != null)
                            return true;
                    }

                    return false;
                default:
                    path = context.RequestPath();
                    break;
            }

            return _delegateMap.ContainsKey(path);
        }

        // TODO: Riccardo, I commented this because I'm not pretty sure what was the idea
        //private WebHandler GetHandler(IHttpContext context)
        //{
        //    Map handler = null;

        //    void SetHandlerFromRegexPath()
        //    {
        //        handler = Handlers.FirstOrDefault(x =>
        //            (x.Path == ModuleMap.AnyPath || context.RequestRegexUrlParams(x.Path) != null) &&
        //            (x.Verb == HttpVerbs.Any || x.Verb == context.RequestVerb()));
        //    }

        //    void SetHandlerFromWildcardPath()
        //    {
        //        var path = context.RequestWilcardPath(module.Handlers
        //            .Where(k => k.Path.Contains(ModuleMap.AnyPathRoute))
        //            .Select(s => s.Path.ToLowerInvariant()));

        //        handler = Handlers
        //            .FirstOrDefault(x =>
        //                (x.Path == ModuleMap.AnyPath || x.Path == path) &&
        //                (x.Verb == HttpVerbs.Any || x.Verb == context.RequestVerb()));
        //    }

        //    switch (RoutingStrategy)
        //    {
        //        case WebApiRoutingStrategy.Wildcard:
        //            SetHandlerFromWildcardPath();
        //            break;
        //        case WebApiRoutingStrategy.Regex:
        //            SetHandlerFromRegexPath();
        //            break;
        //    }

        //    return handler?.ResponseHandler;
        //}

        /// <inheritdoc />
        public override async Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct)
        {
            var verb = context.RequestVerb();
            var regExRouteParams = new Dictionary<string, object>();
            path = RoutingStrategy == WebApiRoutingStrategy.Wildcard
                ? NormalizeWildcardPath(verb, context, path)
                : NormalizeRegexPath(verb, context, regExRouteParams);

            // Return a non-path if no handler handled the route
            /// TODO: Distinguish between method not allowed and path not found
            if (path == null)
                return await OnMethodNotAllowedAsync( context, path, ct).ConfigureAwait(false);

            // search the path and verb
            if (!_delegateMap.TryGetValue(path, out var methods) ||
                !methods.TryGetValue(verb, out var methodPair))
                throw new InvalidOperationException($"No method found for path {path} and verb {verb}.");

            // ensure module does not return cached responses by default or the custom headers
            var controller = methodPair.SetDefaultHeaders(context, ct);

            // Log the handler to be use
            $"Handler: {methodPair.MethodCache.ControllerName}.{methodPair.MethodCache.MethodInfo.Name}"
                .Debug(nameof(WebApiModule));

            // Initially, only the server and context objects will be available
            var args = new object[methodPair.MethodCache.AdditionalParameters.Count];

            if (RoutingStrategy == WebApiRoutingStrategy.Regex)
                methodPair.ParseArguments(regExRouteParams, args);

            try
            {
                return await methodPair.Invoke(controller, args).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ex.Log(GetType().Name);
                return await OnExceptionAsync(context, path, ex, ct).ConfigureAwait(false);
            }
            finally
            {
                (controller as IDisposable)?.Dispose();
            }
        }

        /// <summary>
        /// <para>Called when no controller is found for the requested URL path.</para>
        /// <para>The default behavior is to send an empty <c>404 Not Found</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="BaseUrlPath"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        protected virtual Task<bool> OnPathNotFoundAsync(IHttpContext context, string path, CancellationToken ct)
        {
            context.Response.StandardResponseWithoutBody((int)HttpStatusCode.NotFound);
            return Task.FromResult(true);
        }

        /// <summary>
        /// <para>Called when a controller is found for the requested URL path,
        /// but it does not handle the HTTP method of the request.</para>
        /// <para>The default behavior is to send an empty <c>405 Method Not Allowed</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="BaseUrlPath"/>.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        protected virtual Task<bool> OnMethodNotAllowedAsync(IHttpContext context, string path, CancellationToken ct)
        {
            context.Response.StandardResponseWithoutBody((int)HttpStatusCode.MethodNotAllowed);
            return Task.FromResult(true);
        }

        /// <summary>
        /// <para>Called when a controller throws an exception while handling a request.</para>
        /// <para>The default behavior is to send a <c>500 Internal Server Error</c> response,
        /// optionally containing a JSON description of the exception, according to the value of
        /// the <see cref="SendJsonOnException"/> property.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="BaseUrlPath"/>.</param>
        /// <param name="exception">The exception thrown by the controller.</param>
        /// <param name="ct">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        protected virtual Task<bool> OnExceptionAsync(IHttpContext context, string path, Exception exception, CancellationToken ct)
        {
            if (SendJsonOnException)
                return context.JsonExceptionResponseAsync(exception, cancellationToken: ct);

            context.Response.StandardResponseWithoutBody((int)HttpStatusCode.InternalServerError);
            return Task.FromResult(true);
        }
    }
}
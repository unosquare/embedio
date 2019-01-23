namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Constants;
    using EmbedIO;
    using Swan;
    
    /// <summary>
    /// A very simple module to register class methods as handlers.
    /// Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests.
    /// </summary>
    public class WebApiModule
        : WebModuleBase
    {
        private readonly List<Type> _controllerTypes = new List<Type>();

        private readonly Dictionary<string, Dictionary<HttpVerbs, MethodCacheInstance>> _delegateMap
            =
            new Dictionary<string, Dictionary<HttpVerbs, MethodCacheInstance>>(
                Strings.StandardStringComparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiModule"/> class.
        /// </summary>
        public WebApiModule()
        {
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Any, async (context, ct) =>
            {
                var verb = context.RequestVerb();
                var regExRouteParams = new Dictionary<string, object>();
                var path = Server.RoutingStrategy == RoutingStrategy.Wildcard
                    ? NormalizeWildcardPath(verb, context)
                    : NormalizeRegexPath(verb, context, regExRouteParams);

                // return a non-math if no handler hold the route
                if (path == null)
                {
                    return IsMethodNotAllowed(context) && Server.OnMethodNotAllowed != null && await Server.OnMethodNotAllowed(context).ConfigureAwait(false);
                }

                // search the path and verb
                if (!_delegateMap.TryGetValue(path, out var methods) ||
                    !methods.TryGetValue(verb, out var methodPair))
                    throw new InvalidOperationException($"No method found for path {path} and verb {verb}.");

                // ensure module does not return cached responses by default or the custom headers
                methodPair.SetDefaultHeaders(context);

                // Log the handler to be use
                $"Handler: {methodPair.MethodCache.ControllerName}.{methodPair.MethodCache.MethodInfo.Name}"
                    .Debug(nameof(WebApiModule));

                // Initially, only the server and context objects will be available
                var args = new object[methodPair.MethodCache.AdditionalParameters.Count];

                if (Server.RoutingStrategy == RoutingStrategy.Regex)
                    methodPair.ParseArguments(regExRouteParams, args);

                return await methodPair.Invoke(context, args).ConfigureAwait(false);
            });
        }

        /// <inheritdoc />
        public override string Name => "Web API Module";

        /// <summary>
        /// Gets the number of controller objects registered in this API.
        /// </summary>
        public int ControllersCount => _controllerTypes.Count;

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="T">The type of register controller.</typeparam>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module.</exception>
        public void RegisterController<T>()
            where T : WebApiController
        {
            RegisterController(typeof(T));
        }

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="T">The type of register controller.</typeparam>
        /// <param name="controllerFactory">The controller factory method.</param>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module.</exception>
        public void RegisterController<T>(Func<IHttpContext, T> controllerFactory)
            where T : WebApiController
        {
            RegisterController(typeof(T), controllerFactory);
        }

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <param name="controllerType">Type of the controller.</param>
        public void RegisterController(Type controllerType)
            => RegisterController(controllerType, (ctx) => Activator.CreateInstance(controllerType, ctx));

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <param name="controllerType">Type of the controller.</param>
        /// <param name="controllerFactory">The controller factory method.</param>
        public void RegisterController(Type controllerType, Func<IHttpContext, object> controllerFactory)
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

                    var delegatePair = new MethodCacheInstance(controllerFactory, new MethodCache(method));

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
            var path = context.Request.Url.LocalPath;

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
        private string NormalizeWildcardPath(HttpVerbs verb, IHttpContext context)
        {
            var path = context.RequestWilcardPath(_delegateMap.Keys
                .Where(k => k.Contains(ModuleMap.AnyPathRoute))
                .Select(s => s.ToLowerInvariant()));

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

            switch (Server.RoutingStrategy)
            {
                case RoutingStrategy.Wildcard:
                    path = context.RequestWilcardPath(_delegateMap.Keys
                        .Where(k => k.Contains(ModuleMap.AnyPathRoute))
                        .Select(s => s.ToLowerInvariant()));
                    break;
                case RoutingStrategy.Regex:
                    path = context.Request.Url.LocalPath;
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
    }
}
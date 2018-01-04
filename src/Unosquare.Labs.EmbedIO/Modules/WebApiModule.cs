namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using EmbedIO;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Swan;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// A delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns><b>true</b> if the response was completed, otherwise <b>false</b></returns>
    internal delegate bool ResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// An async delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns>A task with <b>true</b> if the response was completed, otherwise <b>false</b></returns>
    internal delegate Task<bool> AsyncResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// A very simple module to register class methods as handlers.
    /// Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests
    /// </summary>
    public class WebApiModule : WebModuleBase
    {
        #region Immutable Declarations

        private readonly List<Type> _controllerTypes = new List<Type>();

        private readonly Dictionary<string, Dictionary<HttpVerbs, MethodCacheInstance>> _delegateMap
            =
            new Dictionary<string, Dictionary<HttpVerbs, MethodCacheInstance>>(
                Strings.StandardStringComparer);

        #endregion

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
                    if (path == null) return false;

                    // search the path and verb
                    if (!_delegateMap.TryGetValue(path, out var methods) ||
                        !methods.TryGetValue(verb, out var methodPair))
                        throw new InvalidOperationException($"No method found for path {path} and verb {verb}.");

                    // ensure module does not return cached responses
                    context.NoCache();

                    // Log the handler to be use
                    $"Handler: {methodPair.MethodCache.MethodInfo.DeclaringType?.FullName}.{methodPair.MethodCache.MethodInfo.Name}"
                        .Debug(nameof(WebApiModule));

                    // Initially, only the server and context objects will be available
                    var args = new object[methodPair.MethodCache.AdditionalParameters.Count + 2];
                    args[0] = Server;
                    args[1] = context;

                    // Select the routing strategy
                    switch (Server.RoutingStrategy)
                    {
                        case RoutingStrategy.Regex:
                            methodPair.ParseArguments(regExRouteParams, args);
                            return await methodPair.Invoke(args);
                        case RoutingStrategy.Wildcard:
                            return await methodPair.Invoke(args);
                        default:
                            // Log the handler to be used
                            $"Routing strategy '{Server.RoutingStrategy}' is not supported by this module.".Warn(
                                nameof(WebApiModule));
                            return false;
                    }
                });
        }

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "Web API Module";
        
        /// <summary>
        /// Gets the number of controller objects registered in this API
        /// </summary>
        public int ControllersCount => _controllerTypes.Count;

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="T">The type of register controller</typeparam>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module</exception>
        public void RegisterController<T>()
            where T : WebApiController, new()
        {
            RegisterController(typeof(T));
        }

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="T">The type of register controller</typeparam>
        /// <param name="controllerFactory">The controller factory method</param>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module</exception>
        public void RegisterController<T>(Func<T> controllerFactory)
            where T : WebApiController
        {
            RegisterController(typeof(T), controllerFactory);
        }

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <param name="controllerType">Type of the controller.</param>
        public void RegisterController(Type controllerType)
            => RegisterController(controllerType, () => Activator.CreateInstance(controllerType));

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <param name="controllerType">Type of the controller.</param>
        /// <param name="controllerFactory">The controller factory method.</param>
        public void RegisterController(Type controllerType, Func<object> controllerFactory)
        {
            if (_controllerTypes.Contains(controllerType))
                throw new ArgumentException("Controller types must be unique within the module");

            var protoDelegate = new ResponseHandler((server, context) => true);
            var protoAsyncDelegate = new AsyncResponseHandler((server, context) => Task.FromResult(true));
            var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(
                    m => (m.ReturnType == protoDelegate.GetMethodInfo().ReturnType
                          || m.ReturnType == protoAsyncDelegate.GetMethodInfo().ReturnType)
                         && m.GetParameters()
                             .Select(pi => pi.ParameterType)
                             .Take(2)
                             .SequenceEqual(protoDelegate.GetMethodInfo().GetParameters()
                                 .Select(pi => pi.ParameterType)));

            foreach (var method in methods)
            {
                var attribute =
                    method.GetCustomAttributes(typeof(WebApiHandlerAttribute), true).FirstOrDefault() as
                        WebApiHandlerAttribute;
                if (attribute == null) continue;

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
        /// <returns>A string that represents the registered path in the internal delegate map</returns>
        private string NormalizeRegexPath(
            HttpVerbs verb,
            HttpListenerContext context,
            Dictionary<string, object> routeParams)
        {
            var path = context.Request.Url.LocalPath;

            foreach (var route in _delegateMap.Keys)
            {
                var urlParam = EmbedIO.Extensions.RequestRegexUrlParams(path, route, () => !_delegateMap[route].Keys.Contains(verb));

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
        /// <returns>A string that represents the registered path</returns>
        private string NormalizeWildcardPath(HttpVerbs verb, HttpListenerContext context)
        {
            var path = context.RequestWilcardPath(_delegateMap.Keys
                .Where(k => k.Contains("/" + ModuleMap.AnyPath))
                .Select(s => s.ToLowerInvariant())
                .ToArray());

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
    }

    /// <summary>
    /// Inherit from this class and define your own Web API methods
    /// You must RegisterController in the Web API Module to make it active
    /// </summary>
    public abstract class WebApiController
    {
    }
}
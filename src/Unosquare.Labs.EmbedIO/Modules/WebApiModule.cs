namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using System.Text.RegularExpressions;
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
    public delegate bool ResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// An async delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns>A task with <b>true</b> if the response was completed, otherwise <b>false</b></returns>
    public delegate Task<bool> AsyncResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// A very simple module to register class methods as handlers.
    /// Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests
    /// </summary>
    public class WebApiModule : WebModuleBase
    {
        #region Immutable Declarations
        
        private static readonly Regex RouteParamRegex = new Regex(@"\{[^\/]*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RouteOptionalParamRegex = new Regex(@"\{[^\/]*\?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string RegexRouteReplace = "(.*)";

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

                Dictionary<HttpVerbs, MethodCacheInstance> methods;
                MethodCacheInstance methodPair;

                // search the path and verb
                if (!_delegateMap.TryGetValue(path, out methods) || !methods.TryGetValue(verb, out methodPair))
                    throw new InvalidOperationException($"No method found for path {path} and verb {verb}.");
                
                // ensure module does not return cached responses
                context.NoCache();

                // Log the handler to be use
                $"Handler: {methodPair.MethodCache.MethodInfo.DeclaringType?.FullName}.{methodPair.MethodCache.MethodInfo.Name}".Debug(nameof(WebApiModule));

                // Initially, only the server and context objects will be available
                var args = new object[methodPair.MethodCache.AdditionalParameters.Count + 2];
                args[0] = Server;
                args[1] = context;
                
                // Select the routing strategy
                switch (Server.RoutingStrategy)
                {
                    case RoutingStrategy.Regex:

                        // Parse the arguments to their intended type skipping the first two.
                        for (var i = 0; i < methodPair.MethodCache.AdditionalParameters.Count; i++)
                        {
                            var param = methodPair.MethodCache.AdditionalParameters[i];
                            if (regExRouteParams.ContainsKey(param.Info.Name))
                            {
                                var value = (string) regExRouteParams[param.Info.Name];

                                if (string.IsNullOrWhiteSpace(value))
                                    value = null; // ignore whitespace

                                // if the value is null, there's nothing to convert
                                if (value == null)
                                {
                                    // else we use the default value (null for nullable types)
                                    args[i + 2] = param.Default;
                                    continue;
                                }

                                // convert and add to arguments
                                args[i + 2] = param.Converter.ConvertFromString(value);
                            }
                            else
                            {
                                args[i + 2] = param.Default;
                            }
                        }

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
        {
            Func<object> controllerFactory = () => Activator.CreateInstance(controllerType);
            RegisterController(controllerType, controllerFactory);
        }

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
                var regex = new Regex(RouteParamRegex.Replace(route, RegexRouteReplace));
                var match = regex.Match(path);

                var pathParts = route.Split('/');

                if (!match.Success || !_delegateMap[route].Keys.Contains(verb))
                {
                    var optionalPath = RouteOptionalParamRegex.Replace(route, string.Empty);
                    var tempPath = path;

                    if (optionalPath.Last() == '/' && path.Last() != '/')
                    {
                        tempPath += "/";
                    }

                    if (optionalPath == tempPath)
                    {
                        foreach (var pathPart in pathParts.Where(x => x.StartsWith("{")))
                        {
                            routeParams.Add(
                                pathPart.Replace("{", string.Empty)
                                    .Replace("}", string.Empty)
                                    .Replace("?", string.Empty), null);
                        }

                        return route;
                    }

                    continue;
                }

                var i = 1; // match group index

                foreach (var pathPart in pathParts.Where(x => x.StartsWith("{")))
                {
                    routeParams.Add(
                        pathPart.Replace("{", string.Empty).Replace("}", string.Empty).Replace("?", string.Empty),
                        match.Groups[i++].Value);
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
            var path = context.RequestPath();

            var wildcardPaths = _delegateMap.Keys
                .Where(k => k.Contains("/" + ModuleMap.AnyPath))
                .Select(s => s.ToLowerInvariant())
                .ToArray();

            var wildcardMatch = wildcardPaths.FirstOrDefault(p => // wildcard at the end
                path.StartsWith(p.Substring(0, p.Length - ModuleMap.AnyPath.Length))

                // wildcard in the middle so check both start/end
                || (path.StartsWith(p.Substring(0, p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal)))
                    && path.EndsWith(p.Substring(p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal) + 1))));

            if (string.IsNullOrWhiteSpace(wildcardMatch) == false)
                path = wildcardMatch;

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
    /// Decorate methods within controllers with this attribute in order to make them callable from the Web API Module
    /// Method Must match the WebServerModule.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class WebApiHandlerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiHandlerAttribute"/> class.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="paths">The paths.</param>
        /// <exception cref="System.ArgumentException">The argument 'paths' must be specified.</exception>
        public WebApiHandlerAttribute(HttpVerbs verb, string[] paths)
        {
            if (paths == null || paths.Length == 0)
                throw new ArgumentException("The argument 'paths' must be specified.");

            Verb = verb;
            Paths = paths;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiHandlerAttribute"/> class.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentException">The argument 'path' must be specified.</exception>
        public WebApiHandlerAttribute(HttpVerbs verb, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("The argument 'path' must be specified.");

            Verb = verb;
            Paths = new[] {path};
        }

        /// <summary>
        /// Gets or sets the verb.
        /// </summary>
        /// <value>
        /// The verb.
        /// </value>
        public HttpVerbs Verb { get; protected set; }

        /// <summary>
        /// Gets or sets the paths.
        /// </summary>
        /// <value>
        /// The paths.
        /// </value>
        public string[] Paths { get; protected set; }
    }

    /// <summary>
    /// Inherit from this class and define your own Web API methods
    /// You must RegisterController in the Web API Module to make it active
    /// </summary>
    public abstract class WebApiController
    {
    }
}
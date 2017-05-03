namespace Unosquare.Labs.EmbedIO.Modules
{
    using System.Text.RegularExpressions;
    using EmbedIO;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Swan;
#if NET46
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// A delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public delegate bool ResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// An async delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    /// <returns></returns>
    public delegate Task<bool> AsyncResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// A very simple module to register class methods as handlers.
    /// Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests
    /// </summary>
    public class WebApiModule : WebModuleBase
    {
        #region Immutable Declarations

        private readonly List<Type> _controllerTypes = new List<Type>();

        private readonly Dictionary<string, Dictionary<HttpVerbs, Tuple<Func<object>, MethodInfo>>> _delegateMap
            =
            new Dictionary<string, Dictionary<HttpVerbs, Tuple<Func<object>, MethodInfo>>>(
                Constants.StandardStringComparer);

        private static readonly Regex RouteParamRegex = new Regex(@"\{[^\/]*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RouteOptionalParamRegex = new Regex(@"\{[^\/]*\?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string RegexRouteReplace = "(.*)";

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

                var methodPair = _delegateMap[path][verb];
                var controller = methodPair.Item1();

                // ensure module does not return cached responses
                context.NoCache();

                // Log the handler to be use
                $"Handler: {methodPair.Item2.DeclaringType?.FullName}.{methodPair.Item2.Name}".Debug(nameof(WebApiModule));

                // Initially, only the server and context objects will be available
                var args = new List<object>() { Server, context };

                // Select the routing strategy
                switch (Server.RoutingStrategy)
                {
                    case RoutingStrategy.Regex:

                        // Parse the arguments to their intended type skipping the first two.
                        foreach (var arg in methodPair.Item2.GetParameters().Skip(2))
                        {
                            if (regExRouteParams.ContainsKey(arg.Name) == false) continue;
                            // get a reference to the parse method
                            var parameterTypeNullable = Nullable.GetUnderlyingType(arg.ParameterType);

#if NETSTANDARD1_3 || UWP
                            var parseMethod = parameterTypeNullable != null
                                ? parameterTypeNullable.GetMethod(nameof(int.Parse), new[] { typeof(string) })
                                : arg.ParameterType.GetMethod(nameof(int.Parse), new[] { typeof(string) });
#else
                            var parseMethod = parameterTypeNullable != null
                                ? parameterTypeNullable.GetTypeInfo()
                                    .GetMethod(nameof(int.Parse), new[] {typeof(string)})
                                : arg.ParameterType.GetTypeInfo().GetMethod(nameof(int.Parse), new[] {typeof(string)});
#endif

                            // add the parsed argument to the argument list if available
                            if (parseMethod != null)
                            {
                                // parameter is nullable and value is empty, so force null
                                if (parameterTypeNullable != null &&
                                    string.IsNullOrWhiteSpace((string) regExRouteParams[arg.Name]))
                                {
                                    args.Add(null);
                                }
                                else
                                {
                                    args.Add(parseMethod.Invoke(null, new[] {regExRouteParams[arg.Name]}));
                                }
                            }
                            else
                            {
                                args.Add(regExRouteParams[arg.Name]);
                            }
                        }

                        // Now, check if the call is handled asynchronously.
                        if (methodPair.Item2.ReturnType == typeof(Task<bool>))
                        {
                            // Run the method asynchronously
                            return await (Task<bool>) methodPair.Item2.Invoke(controller, args.ToArray());
                        }
                        
                        // If the handler is not asynchronous, simply call the method.
                        return (bool) methodPair.Item2.Invoke(controller, args.ToArray());
                    case RoutingStrategy.Wildcard:
                        if (methodPair.Item2.ReturnType == typeof(Task<bool>))
                        {
                            // Asynchronous handling of wildcard matching strategy
                            var method = methodPair.Item2.CreateDelegate(typeof(AsyncResponseHandler), controller);

                            return await (Task<bool>) method.DynamicInvoke(args.ToArray());
                        }
                        else
                        {
                            // Regular handling of wildcard matching strategy
                            var method = methodPair.Item2.CreateDelegate(typeof(ResponseHandler), controller);

                            return (bool) method.DynamicInvoke(args.ToArray());
                        }
                    default:
                        // Log the handler to be used
                        $"Routing strategy '{Server.RoutingStrategy}' is not supported by this module.".Warn(
                            nameof(WebApiModule));
                        return false;
                }
            });
        }

        /// <summary>
        /// Normalizes a path meant for Regex matching, extracts the route parameters, and returns the registered
        /// path in the internal delegate map.
        /// </summary>
        /// <param name="verb">The verb.</param>
        /// <param name="context">The context.</param>
        /// <param name="routeParams">The route parameters.</param>
        /// <returns></returns>
        private string NormalizeRegexPath(HttpVerbs verb, HttpListenerContext context,
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
        /// <returns></returns>
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
                    && path.EndsWith(p.Substring(p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal) + 1)))
            );

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
        /// <typeparam name="T"></typeparam>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module</exception>
        public void RegisterController<T>()
            where T : WebApiController, new()
        {
            if (_controllerTypes.Contains(typeof(T)))
                throw new ArgumentException("Controller types must be unique within the module");

            RegisterController(typeof(T));
        }

        /// <summary>
        /// Registers the controller.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controllerFactory"></param>
        /// <exception cref="System.ArgumentException">Controller types must be unique within the module</exception>
        public void RegisterController<T>(Func<T> controllerFactory)
            where T : WebApiController
        {
            if (_controllerTypes.Contains(typeof(T)))
                throw new ArgumentException("Controller types must be unique within the module");

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
                        _delegateMap.Add(path, new Dictionary<HttpVerbs, Tuple<Func<object>, MethodInfo>>()); // add
                    }

                    var delegatePair = new Tuple<Func<object>, MethodInfo>(controllerFactory, method);

                    if (_delegateMap[path].ContainsKey(attribute.Verb))
                        _delegateMap[path][attribute.Verb] = delegatePair; // update
                    else
                        _delegateMap[path].Add(attribute.Verb, delegatePair); // add
                }
            }

            _controllerTypes.Add(controllerType);
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
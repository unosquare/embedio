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
    using System.ComponentModel;
    using System.Linq.Expressions;

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
    public delegate bool ResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// An async delegate that handles certain action in a module given a path and a verb
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="context">The context.</param>
    public delegate Task<bool> AsyncResponseHandler(WebServer server, HttpListenerContext context);

    /// <summary>
    /// A very simple module to register class methods as handlers.
    /// Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests
    /// </summary>
    public class WebApiModule : WebModuleBase
    {
        #region Immutable Declarations

        private const string RegexRouteReplace = "(.*)";

        private readonly List<Type> _controllerTypes = new List<Type>();

        private readonly Dictionary<string, Dictionary<HttpVerbs, Tuple<Func<object>, MethodCache>>> _delegateMap
            =
            new Dictionary<string, Dictionary<HttpVerbs, Tuple<Func<object>, MethodCache>>>(
                Strings.StandardStringComparer);

        private static readonly Regex RouteParamRegex = new Regex(@"\{[^\/]*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RouteOptionalParamRegex = new Regex(@"\{[^\/]*\?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);        

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

                Dictionary<HttpVerbs, Tuple<Func<object>, MethodCache>> methods;
                Tuple<Func<object>, MethodCache> methodPair;

                // search the path and verb
                if (!_delegateMap.TryGetValue(path, out methods) || !methods.TryGetValue(verb, out methodPair))
                    throw new InvalidOperationException($"No method found for path {path} and verb {verb}.");

                var controller = methodPair.Item1();

                // ensure module does not return cached responses
                context.NoCache();

                // Log the handler to be use
                $"Handler: {methodPair.Item2.MethodInfo.DeclaringType?.FullName}.{methodPair.Item2.MethodInfo.Name}".Debug(nameof(WebApiModule));

                // Initially, only the server and context objects will be available
                var args = new object[methodPair.Item2.AdditionalParameters.Count + 2];
                args[0] = Server;
                args[1] = context;
                
                // Select the routing strategy
                switch (Server.RoutingStrategy)
                {
                    case RoutingStrategy.Regex:

                        // Parse the arguments to their intended type skipping the first two.
                        for (var i = 0; i < methodPair.Item2.AdditionalParameters.Count; i++)
                        {
                            var param = methodPair.Item2.AdditionalParameters[i];
                            if (regExRouteParams.ContainsKey(param.Info.Name))
                            {
                                var value = (string) regExRouteParams[param.Info.Name];
                                if (string.IsNullOrWhiteSpace(value))
                                    value = null; //ignore whitespace

                                //if the value is null, there's nothing to convert
                                if (value == null)
                                {
                                    //else we use the default value (null for nullable types)
                                    args[i + 2] = param.Default;
                                    continue;
                                }

                                //convert and add to arguments
                                args[i + 2] = param.Converter.ConvertFromString(value);
                            }
                            else
                                args[i + 2] = param.Default;
                        }

                        // Now, check if the call is handled asynchronously.
                        if (methodPair.Item2.IsTask)
                        {
                            // Run the method asynchronously
                            return await methodPair.Item2.AsyncInvoke(controller, args);
                        }
                        
                        // If the handler is not asynchronous, simply call the method.
                        return methodPair.Item2.SyncInvoke(controller, args);
                    case RoutingStrategy.Wildcard:
                        if (methodPair.Item2.IsTask)
                        {
                            // Asynchronous handling of wildcard matching strategy
                            var method = methodPair.Item2.MethodInfo.CreateDelegate(typeof(AsyncResponseHandler), controller);

                            return await (Task<bool>) method.DynamicInvoke(args.ToArray());
                        }
                        else
                        {
                            // Regular handling of wildcard matching strategy
                            var method = methodPair.Item2.MethodInfo.CreateDelegate(typeof(ResponseHandler), controller);

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
            if (_controllerTypes.Contains(typeof(T)))
                throw new ArgumentException("Controller types must be unique within the module");

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
                        _delegateMap.Add(path, new Dictionary<HttpVerbs, Tuple<Func<object>, MethodCache>>()); // add
                    }

                    var delegatePair = new Tuple<Func<object>, MethodCache>(controllerFactory, new MethodCache(method));

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

    internal class MethodCache
    {
        public delegate Task<bool> AsyncDelegate(object instance, object[] arguments);
        public delegate bool SyncDelegate(object instance, object[] arguments);

        public MethodCache(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            IsTask = methodInfo.ReturnType == typeof(Task<bool>);
            AdditionalParameters = methodInfo.GetParameters().Skip(2).Select(x => new AddtionalParameterInfo(x))
                .ToList();

            var invokeDelegate = BuildDelegate(methodInfo, IsTask);
            if (IsTask)
                AsyncInvoke = (AsyncDelegate) invokeDelegate;
            else
                SyncInvoke = (SyncDelegate) invokeDelegate;
        }

        public MethodInfo MethodInfo { get; }
        public bool IsTask { get; }
        public List<AddtionalParameterInfo> AdditionalParameters { get; }

        public AsyncDelegate AsyncInvoke { get; }
        public SyncDelegate SyncInvoke { get; }

        private static Delegate BuildDelegate(MethodInfo methodInfo, bool isAsync)
        {
            var instanceExpression = Expression.Parameter(typeof(object), "instance");
            var argumentsExpression = Expression.Parameter(typeof(object[]), "arguments");
            var argumentExpressions = new List<Expression>();
            var parameterInfos = methodInfo.GetParameters();

            for (var i = 0; i < parameterInfos.Length; ++i)
            {
                var parameterInfo = parameterInfos[i];
                argumentExpressions.Add(Expression.Convert(
                    Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType));
            }

            var callExpression = Expression.Call(Expression.Convert(instanceExpression, methodInfo.ReflectedType),
                methodInfo, argumentExpressions);

            if (isAsync)
                return
                    Expression.Lambda<AsyncDelegate>(Expression.Convert(callExpression, typeof(Task<bool>)),
                        instanceExpression, argumentsExpression).Compile();

            return Expression.Lambda<SyncDelegate>(Expression.Convert(callExpression, typeof(bool)),
                instanceExpression, argumentsExpression).Compile();
        }
    }

    internal class AddtionalParameterInfo
    {
        public AddtionalParameterInfo(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            Converter = TypeDescriptor.GetConverter(parameterInfo.ParameterType);

            if (parameterInfo.ParameterType.IsValueType)
                Default = Activator.CreateInstance(parameterInfo.ParameterType);
        }

        public object Default { get; }
        public ParameterInfo Info { get; }
        public TypeConverter Converter { get; }
    }
}
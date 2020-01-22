using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Swan.Logging;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Handles a HTTP request by matching it against a list of routes,
    /// possibly handling different HTTP methods via different handlers.
    /// </summary>
    /// <seealso cref="RouteResolverBase{TData}"/>
    /// <seealso cref="RouteVerbResolver"/>
    public sealed class RouteVerbResolverCollection : RouteResolverCollectionBase<HttpVerbs, RouteVerbResolver>
    {
        private readonly string _logSource;

        internal RouteVerbResolverCollection(string logSource)
        {
            _logSource = logSource;
        }

        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteAttribute">Route</see> attributes.</para>
        /// <para>A compatible handler is a static or instance method that takes 2
        /// parameters having the following types, in order:</para>
        /// <list type="number">
        /// <item><description><see cref="IHttpContext"/></description></item>
        /// <item><description><see cref="RouteMatch"/></description></item>
        /// </list>
        /// <para>The return type of a compatible handler may be either <see langword="void"/>
        /// or <see cref="Task"/>.</para>
        /// <para>A compatible handler, in order to be added to a <see cref="RouteVerbResolverCollection"/>,
        /// must have one or more <see cref="RouteAttribute">Route</see> attributes.
        /// The same handler will be added once for each such attribute, either declared on the handler,
        /// or inherited (if the handler is a virtual method).</para>
        /// <para>This method behaves according to the type of the <paramref name="target"/>
        /// parameter:</para>
        /// <list type="bullet">
        /// <item><description>if <paramref name="target"/> is a <see cref="Type"/>, all public static methods of
        /// the type (either declared on the same type or inherited) that are compatible handlers will be added
        /// to the collection;</description></item>
        /// <item><description>if <paramref name="target"/> is an <see cref="Assembly"/>, all public static methods of
        /// each exported type of the assembly (either declared on the same type or inherited) that are compatible handlers will be added
        /// to the collection;</description></item>
        /// <item><description>if <paramref name="target"/> is a <see cref="MethodInfo"/> referring to a compatible handler,
        /// it will be added to the collection;</description></item>
        /// <item><description>if <paramref name="target"/> is a <see langword="delegate"/> whose <see cref="Delegate.Method">Method</see>
        /// refers to a compatible handler, that method will be added to the collection;</description></item>
        /// <item><description>if <paramref name="target"/> is none of the above, all public instance methods of
        /// its type (either declared on the same type or inherited) that are compatible handlers will be bound to <paramref name="target"/>
        /// and added to the collection.</description></item>
        /// </list>
        /// </summary>
        /// <param name="target">Where to look for compatible handlers. See the Summary section for more information.</param>
        /// <returns>
        /// <para>The number of handlers that were added to the collection.</para>
        /// <para>Note that methods with multiple <see cref="RouteAttribute">Route</see> attributes
        /// will count as one for each attribute.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        public int AddFrom(object target) => Validate.NotNull(nameof(target), target) switch {
            Type type => AddFrom(null, type),
            Assembly assembly => assembly.GetExportedTypes().Sum(t => AddFrom(null, t)),
            MethodInfo method => method.IsStatic ? Add(null, method) : 0,
            Delegate callback => Add(callback.Target, callback.Method),
            _ => AddFrom(target, target.GetType())
        };

        /// <inheritdoc />
        protected override RouteVerbResolver CreateResolver(RouteMatcher matcher) => new RouteVerbResolver(matcher);

        /// <inheritdoc />
        protected override void OnResolverCalled(IHttpContext context, RouteVerbResolver resolver, RouteResolutionResult result)
            => $"[{context.Id}] Route {resolver.Route} : {result}".Trace(_logSource);

        private static bool IsHandlerCompatibleMethod(MethodInfo method, out bool isSynchronous)
        {
            isSynchronous = false;
            var returnType = method.ReturnType;
            if (returnType == typeof(void))
            {
                isSynchronous = true;
            }
            else if (returnType != typeof(Task))
            {
                return false;
            }

            var parameters = method.GetParameters();
            return parameters.Length == 2
                && parameters[0].ParameterType.IsAssignableFrom(typeof(IHttpContext))
                && parameters[1].ParameterType.IsAssignableFrom(typeof(RouteMatch));
        }

        // Call Add with all suitable methods of a Type, return sum of results.
        private int AddFrom(object? target, Type type)
            => type.GetMethods(target == null
                    ? BindingFlags.Public | BindingFlags.Static
                    : BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.IsPublic
                              && !method.IsAbstract
                              && !method.ContainsGenericParameters)
                .Sum(m => Add(target, m));

        private int Add(object? target, MethodInfo method)
        {
            if (!IsHandlerCompatibleMethod(method, out var isSynchronous))
                return 0;

            var attributes = method.GetCustomAttributes(true).OfType<RouteAttribute>().ToArray();
            if (attributes.Length == 0)
                return 0;

            var parameters = new[] {
                Expression.Parameter(typeof(IHttpContext), "context"),
                Expression.Parameter(typeof(RouteMatch), "route"),
            };

            Expression body = Expression.Call(Expression.Constant(target), method, parameters.Cast<Expression>());
            if (isSynchronous)
            {
                // Convert void to Task by evaluating Task.CompletedTask
                body = Expression.Block(typeof(Task), body, Expression.Constant(Task.CompletedTask));
            }

            var handler = Expression.Lambda<RouteHandlerCallback>(body, parameters).Compile();
            foreach (var attribute in attributes)
            {
                Add(attribute.Verb, attribute.Matcher, handler);
            }

            return attributes.Length;
        }
    }
}
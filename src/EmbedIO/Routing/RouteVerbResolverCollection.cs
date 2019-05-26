using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Routing
{
    /// <summary>
    /// Handles a HTTP request by matching it against a list of routes,
    /// possibly handling different HTTP methods via different handlers.
    /// </summary>
    /// <seealso cref="RouteResolverBase{TContext,TData}"/>
    /// <seealso cref="RouteVerbResolver"/>
    public sealed class RouteVerbResolverCollection : RouteResolverCollectionBase<IHttpContext, HttpVerbs, RouteVerbResolver>
    {
        private static readonly MethodInfo TaskFromResult = new Func<bool, Task<bool>>(Task.FromResult).Method;

        private readonly string _logSource;

        internal RouteVerbResolverCollection(string logSource)
        {
            _logSource = logSource;
        }

        /// <summary>
        /// <para>Adds handlers, associating them with HTTP method / route pairs by means
        /// of <see cref="RouteHandlerAttribute">RouteHandler</see> attributes.</para>
        /// <para>A compatible handler is a static or instance method that takes 3
        /// parameters having the following types, in order:</para>
        /// <list type="number">
        /// <item><description><see cref="IHttpContext"/></description></item>
        /// <item><description><see cref="RouteMatch"/></description></item>
        /// <item><description><see cref="CancellationToken"/></description></item>
        /// </list>
        /// <para>The return type of a compatible handler may be either <see langword="bool"/>
        /// or <see cref="Task{TResult}">Task&lt;bool&gt;</see>.</para>
        /// <para>A compatible handler, in order to be added to a <see cref="RouteVerbResolverCollection"/>,
        /// must have one or more <see cref="RouteHandlerAttribute">RouteHandler</see> attributes.
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
        /// <para>Note that methods with multiple <see cref="RouteHandlerAttribute">RouteHandler</see> attributes
        /// will count as one for each attribute.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <see langword="null"/>.</exception>
        public int AddFrom(object target)
        {
            switch (Validate.NotNull(nameof(target), target))
            {
                case Type type:
                    return AddFrom(null, type);
                case Assembly assembly:
                    return assembly.GetExportedTypes().Sum(t => AddFrom(null, t));
                case MethodInfo method:
                    return method.IsStatic ? Add(null, method) : 0;
                case Delegate callback:
                    return Add(callback.Target, callback.Method);
                default:
                    return AddFrom(target, target.GetType());
            }
        }

        /// <inheritdoc />
        protected override RouteVerbResolver CreateResolver(string route) => new RouteVerbResolver(route);

        /// <inheritdoc />
        protected override void OnResolverCalled(IHttpContext context, RouteVerbResolver resolver, RouteResolutionResult result)
            => $"[{context.Id}] Route {resolver.Route} : {result}".Debug(_logSource);

        private static bool IsHandlerCompatibleMethod(MethodInfo method, out bool isSynchronous)
        {
            isSynchronous = false;
            var returnType = method.ReturnType;
            if (returnType == typeof(bool))
            {
                isSynchronous = true;
            }
            else if (returnType != typeof(Task<bool>))
            {
                return false;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 3)
                return false;

            return parameters[0].ParameterType.IsAssignableFrom(typeof(IHttpContext))
                && parameters[1].ParameterType.IsAssignableFrom(typeof(RouteMatch))
                && parameters[2].ParameterType.IsAssignableFrom(typeof(CancellationToken));
        }

        // Call Add with all suitable methods of a Type, return sum of results.
        private int AddFrom(object target, Type type)
            => type.GetMethods(target == null
                    ? BindingFlags.Public | BindingFlags.Static
                    : BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.IsPublic
                              && !method.IsAbstract
                              && !method.ContainsGenericParameters)
                .Sum(m => Add(target, m));

        private int Add(object target, MethodInfo method)
        {
            if (!IsHandlerCompatibleMethod(method, out var isSynchronous))
                return 0;

            var attributes = method.GetCustomAttributes(typeof(RouteHandlerAttribute), true).OfType<RouteHandlerAttribute>().ToArray();
            if (attributes.Length == 0)
                return 0;

            var parameters = new[] {
                Expression.Parameter(typeof(IHttpContext), "context"),
                Expression.Parameter(typeof(RouteMatch), "route"),
                Expression.Parameter(typeof(CancellationToken), "cancellationToken"),
            };

            var body = Expression.Call(Expression.Constant(target), method, parameters.Cast<Expression>());
            if (isSynchronous)
            {
                // Convert a bool return type to Task<bool> by passing it to Task.FromResult
                body = Expression.Call(TaskFromResult, body);
            }

            var handler = Expression.Lambda<RouteHandler<IHttpContext>>(body, parameters).Compile();
            foreach (var attribute in attributes)
            {
                Add(attribute.Verb, attribute.Route, handler);
            }

            return attributes.Length;
        }
    }
}
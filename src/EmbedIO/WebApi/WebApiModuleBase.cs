using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Utilities;

namespace EmbedIO.WebApi
{
    /// <summary>
    /// <para>A module using class methods as handlers.</para>
    /// <para>Public instance methods that match the WebServerModule.ResponseHandler signature, and have the WebApi handler attribute
    /// will be used to respond to web server requests.</para>
    /// </summary>
    public abstract class WebApiModuleBase : RoutingModuleBase
    {
        private static readonly MethodInfo TaskFromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult));
        private static readonly MethodInfo PreProcessRequestMethod = typeof(WebApiController).GetMethod(nameof(WebApiController.PreProcessRequest));

        private readonly MethodInfo _onParameterConversionErrorAsyncMethod;

        private readonly HashSet<Type> _controllerTypes = new HashSet<Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiModuleBase" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path served by this module.</param>
        /// <seealso cref="IWebModule.BaseUrlPath" />
        /// <seealso cref="Validate.UrlPath" />
        protected WebApiModuleBase(string baseUrlPath)
            : base(baseUrlPath)
        {
            _onParameterConversionErrorAsyncMethod = new Func<IHttpContext, string, Exception, CancellationToken, Task<bool>>(OnParameterConversionErrorAsync).Method;
        }

        /// <summary>
        /// Gets the number of controller types registered in this module.
        /// </summary>
        public int ControllerCount => _controllerTypes.Count;

        /// <summary>
        /// <para>Registers a controller type using a constructor.</para>
        /// <para>In order for registration to be successful, the specified controller type:</para>
        /// <list type="bullet">
        /// <item><description>must be a subclass of <see cref="WebApiController"/>;</description></item>
        /// <item><description>must not be an abstract class;</description></item>
        /// <item><description>must not be a generic type definition;</description></item>
        /// <item><description>must have a public constructor with two parameters of type <see cref="IHttpContext"/>
        /// and <see cref="CancellationToken"/>, in this order.</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentException">
        /// <para><typeparamref name="TController"/> is already registered in this module.</para>
        /// <para><typeparamref name="TController"/> does not satisfy the prerequisites
        /// listed in the Summary section.</para>
        /// </exception>
        /// <remarks>
        /// <para>A new instance of <typeparamref name="TController"/> will be created
        /// for each request to handle, and dereferenced immediately afterwards,
        /// to be collected during next garbage collection cycle.</para>
        /// <para><typeparamref name="TController"/> is not required to be thread-safe,
        /// as it will be constructed and used in the same synchronization context.
        /// However, since request handling is asynchronous, the actual execution thread
        /// may vary during execution. Care must be exercised when using thread-sensitive
        /// resources or thread-static data.</para>
        /// <para>If <typeparamref name="TController"/> implements <see cref="IDisposable"/>,
        /// its <see cref="IDisposable.Dispose">Dispose</see> method will be called when it has
        /// finished handling a request.</para>
        /// </remarks>
        /// <seealso cref="RegisterControllerType{TController}(Func{IHttpContext,CancellationToken,TController})"/>
        /// <seealso cref="RegisterControllerType(Type)"/>
        protected void RegisterControllerType<TController>()
            where TController : WebApiController
            => RegisterControllerType(typeof(TController));

        /// <summary>
        /// <para>Registers a controller type using a factory method.</para>
        /// <para>In order for registration to be successful:</para>
        /// <list type="bullet">
        /// <item><description><typeparamref name="TController"/> must be a subclass of <see cref="WebApiController"/>;</description></item>
        /// <item><description><typeparamref name="TController"/> must not be an abstract class;</description></item>
        /// <item><description><typeparamref name="TController"/> must not be a generic type definition;</description></item>
        /// <item><description><paramref name="factory"/>'s return type must be either <typeparamref name="TController"/>
        /// or a subclass of <typeparamref name="TController"/>.</description></item>
        /// </list>
        /// </summary>
        /// <typeparam name="TController">The type of the controller.</typeparam>
        /// <param name="factory">The factory method used to construct instances of <typeparamref name="TController"/>.</param>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="factory"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><typeparamref name="TController"/> is already registered in this module.</para>
        /// <para>- or -</para>
        /// <para><paramref name="factory"/> does not satisfy the prerequisites listed in the Summary section.</para>
        /// </exception>
        /// <remarks>
        /// <para><paramref name="factory"/>will be called once for each request to handle
        /// in order to obtain an instance of <typeparamref name="TController"/>.
        /// The returned instance will be dereferenced immediately after handling the request.</para>
        /// <para><typeparamref name="TController"/> is not required to be thread-safe,
        /// as it will be constructed and used in the same synchronization context.
        /// However, since request handling is asynchronous, the actual execution thread
        /// may vary during execution. Care must be exercised when using thread-sensitive
        /// resources or thread-static data.</para>
        /// <para>If <typeparamref name="TController"/> implements <see cref="IDisposable"/>,
        /// its <see cref="IDisposable.Dispose">Dispose</see> method will be called when it has
        /// finished handling a request. In this case it is recommended that
        /// <paramref name="factory"/> return a newly-constructed instance of <typeparamref name="TController"/>
        /// at each invocation.</para>
        /// <para>If <typeparamref name="TController"/> does not implement <see cref="IDisposable"/>,
        /// <paramref name="factory"/> may employ techniques such as instance pooling to avoid
        /// the overhead of constructing a new instance of <typeparamref name="TController"/>
        /// at each invocation. If so, resources such as file handles, database connections, etc.
        /// should be freed before returning from each handler method to avoid
        /// <see href="https://en.wikipedia.org/wiki/Starvation_(computer_science)">starvation</see>.</para>
        /// </remarks>
        /// <seealso cref="RegisterControllerType{TController}()"/>
        /// <seealso cref="RegisterControllerType(Type,Func{IHttpContext,CancellationToken,WebApiController})"/>
        protected void RegisterControllerType<TController>(Func<IHttpContext, CancellationToken, TController> factory)
            where TController : WebApiController
            => RegisterControllerType(typeof(TController), factory);

        /// <summary>
        /// <para>Registers a controller type using a constructor.</para>
        /// <para>In order for registration to be successful, the specified <paramref name="controllerType"/>: </para>
        /// <list type="bullet">
        /// <item><description>must be a subclass of <see cref="WebApiController"/>;</description></item>
        /// <item><description>must not be an abstract class;</description></item>
        /// <item><description>must not be a generic type definition;</description></item>
        /// <item><description>must have a public constructor with two parameters of type <see cref="IHttpContext"/>
        /// and <see cref="CancellationToken"/>, in this order.</description></item>
        /// </list>
        /// </summary>
        /// <param name="controllerType">The type of the controller.</param>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="controllerType"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="controllerType"/> is already registered in this module.</para>
        /// <para>- or -</para>
        /// <para><paramref name="controllerType"/> does not satisfy the prerequisites
        /// listed in the Summary section.</para>
        /// </exception>
        /// <remarks>
        /// <para>A new instance of <paramref name="controllerType"/> will be created
        /// for each request to handle, and dereferenced immediately afterwards,
        /// to be collected during next garbage collection cycle.</para>
        /// <para><paramref name="controllerType"/> is not required to be thread-safe,
        /// as it will be constructed and used in the same synchronization context.
        /// However, since request handling is asynchronous, the actual execution thread
        /// may vary during execution. Care must be exercised when using thread-sensitive
        /// resources or thread-static data.</para>
        /// <para>If <paramref name="controllerType"/> implements <see cref="IDisposable"/>,
        /// its <see cref="IDisposable.Dispose">Dispose</see> method will be called when it has
        /// finished handling a request.</para>
        /// </remarks>
        /// <seealso cref="RegisterControllerType(Type,Func{IHttpContext,CancellationToken,WebApiController})"/>
        /// <seealso cref="RegisterControllerType{TController}()"/>
        protected void RegisterControllerType(Type controllerType)
        {
            EnsureConfigurationNotLocked();

            controllerType = ValidateControllerType(nameof(controllerType), controllerType);

            var constructor = controllerType.GetConstructors().FirstOrDefault(c =>
            {
                var constructorParameters = c.GetParameters();
                return constructorParameters.Length == 2
                    && constructorParameters[0].ParameterType.IsAssignableFrom(typeof(IHttpContext))
                    && constructorParameters[1].ParameterType.IsAssignableFrom(typeof(CancellationToken));
            });
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"Controller type must have a public constructor taking a {nameof(IHttpContext)} and a {nameof(CancellationToken)} as parameters.",
                    nameof(controllerType));
            }

            RegisterControllerInternal(controllerType, (ctx, ct) => Expression.New(constructor, ctx, ct));
        }

        /// <summary>
        /// <para>Registers a controller type using a factory method.</para>
        /// <para>In order for registration to be successful:</para>
        /// <list type="bullet">
        /// <item><description><paramref name="controllerType"/> must be a subclass of <see cref="WebApiController"/>;</description></item>
        /// <item><description><paramref name="controllerType"/> must not be an abstract class;</description></item>
        /// <item><description><paramref name="controllerType"/> must not be a generic type definition;</description></item>
        /// <item><description><paramref name="factory"/>'s return type must be either <paramref name="controllerType"/>
        /// or a subclass of <paramref name="controllerType"/>.</description></item>
        /// </list>
        /// </summary>
        /// <param name="controllerType">The type of the controller.</param>
        /// <param name="factory">The factory method used to construct instances of <paramref name="controllerType"/>.</param>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="controllerType"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="factory"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="controllerType"/> is already registered in this module.</para>
        /// <para>- or -</para>
        /// <para>One or more parameters do not satisfy the prerequisites listed in the Summary section.</para>
        /// </exception>
        /// <remarks>
        /// <para><paramref name="factory"/>will be called once for each request to handle
        /// in order to obtain an instance of <paramref name="controllerType"/>.
        /// The returned instance will be dereferenced immediately after handling the request.</para>
        /// <para><paramref name="controllerType"/> is not required to be thread-safe,
        /// as it will be constructed and used in the same synchronization context.
        /// However, since request handling is asynchronous, the actual execution thread
        /// may vary during execution. Care must be exercised when using thread-sensitive
        /// resources or thread-static data.</para>
        /// <para>If <paramref name="controllerType"/> implements <see cref="IDisposable"/>,
        /// its <see cref="IDisposable.Dispose">Dispose</see> method will be called when it has
        /// finished handling a request. In this case it is recommended that
        /// <paramref name="factory"/> return a newly-constructed instance of <paramref name="controllerType"/>
        /// at each invocation.</para>
        /// <para>If <paramref name="controllerType"/> does not implement <see cref="IDisposable"/>,
        /// <paramref name="factory"/> may employ techniques such as instance pooling to avoid
        /// the overhead of constructing a new instance of <paramref name="controllerType"/>
        /// at each invocation. If so, resources such as file handles, database connections, etc.
        /// should be freed before returning from each handler method to avoid
        /// <see href="https://en.wikipedia.org/wiki/Starvation_(computer_science)">starvation</see>.</para>
        /// </remarks>
        /// <seealso cref="RegisterControllerType(Type)"/>
        /// <seealso cref="RegisterControllerType{TController}(Func{IHttpContext,CancellationToken,TController})"/>
        protected void RegisterControllerType(Type controllerType, Func<IHttpContext, CancellationToken, WebApiController> factory)
        {
            EnsureConfigurationNotLocked();

            controllerType = ValidateControllerType(nameof(controllerType), controllerType);
            factory = Validate.NotNull(nameof(factory), factory);
            if (!controllerType.IsAssignableFrom(factory.Method.ReturnType))
                throw new ArgumentException("Factory method has an incorrect return type.", nameof(factory));

            RegisterControllerInternal(controllerType, (ctx, ct) => Expression.Call(
                factory.Target == null ? null : Expression.Constant(factory.Target),
                factory.Method,
                ctx, 
                ct));
        }

        /// <summary>
        /// <para>Called when EmbedIO fails to convert a route parameter to a handler parameter type.</para>
        /// <para>The default behavior is to send an empty <c>400 Bad Request</c> response.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="name">The name of the route parameter.</param>
        /// <param name="exception">The exception that was thrown.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// <see langword="true" /> if the request has been handled;
        /// <see langword="false" /> if the request should be passed down the module chain.
        /// </returns>
        protected virtual Task<bool> OnParameterConversionErrorAsync(IHttpContext context, string name, Exception exception, CancellationToken cancellationToken)
        {
            context.Response.SetEmptyResponse((int)HttpStatusCode.BadRequest);
            return Task.FromResult(true);
        }

        private static int IndexOfRouteParameter(RouteMatcher matcher, string name)
        {
            var names = matcher.ParameterNames;
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                    return i;
            }

            return -1;
        }

        // Compile a handler.
        //
        // Parameters:
        // - buildFactoryExpression is a callback that, given two Expressions for a IHttpContext
        //   and a CancellationToken, returns an Expression that builds a controller;
        // - method is a MethodInfo for a public instance method of the controller
        //   returning either Task<bool> or bool;
        // - route is the route to which the controller method is associated.
        //
        // This method builds a lambda, with the same signature as a RouteHandler<IHttpContext>, that:
        // - uses the factory Expression to build a controller;
        // - calls the controller method, passing converted route parameters for method parameters with matching names
        //   and default values for other parameters;
        // - if the controller implements IDisposable, disposes it.
        private RouteHandler<IHttpContext> CompileHandler(Func<Expression, Expression, Expression> buildFactoryExpression, MethodInfo method, string route)
        {
            // Parse the route
            var matcher = RouteMatcher.Parse(route);

            // Lambda parameters
            var contextInLambda = Expression.Parameter(typeof(IHttpContext), "context");
            var routeInLambda = Expression.Parameter(typeof(RouteMatch), "route");
            var cancellationTokenInLambda = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            // Local variables
            var locals = new List<ParameterExpression>();

            // Local variable for controller
            var controllerType = method.ReflectedType;
            var controller = Expression.Variable(controllerType, "controller");
            locals.Add(controller);

            // Label for return statement
            var returnTarget = Expression.Label(typeof(Task<bool>));

            // Contents of lambda body
            var bodyContents = new List<Expression>();

            // Build lambda arguments
            var parameters = method.GetParameters();
            var parameterCount = parameters.Length;
            var handlerArguments = new List<Expression>();
            for (var i = 0; i < parameterCount; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType;
                var index = IndexOfRouteParameter(matcher, parameter.Name);
                if (index >= 0)
                {
                    // The name of a route parameter matches the name of a handler parameter.
                    // Convert the parameter to the handler's parameter type.
                    // Do it inside a try / catch block.
                    // On exception call OnParameterConversionErrorAsync and return.
                    var exception = Expression.Variable(typeof(Exception), "exception");
                    var tryBlock = RouteParameterConverter.ConvertExpression(
                        Expression.Property(routeInLambda, "Item", Expression.Constant(index)),
                        parameter.ParameterType);
                    var catchBlock = Expression.Block(
                        Expression.Return(
                            returnTarget,
                            Expression.Call(
                                Expression.Constant(this),
                                _onParameterConversionErrorAsyncMethod,
                                contextInLambda,
                                Expression.Constant(parameter.Name),
                                exception,
                                cancellationTokenInLambda)),
                        Expression.Constant(parameterType.IsValueType
                            ? Activator.CreateInstance(parameterType)
                            : null));
                    handlerArguments.Add(Expression.TryCatch(tryBlock, Expression.Catch(exception, catchBlock)));
                }
                else
                {
                    // No route parameter has the same name as a handler parameter.
                    // Pass the default for the parameter type.
                    handlerArguments.Add(Expression.Constant(parameter.HasDefaultValue
                        ? parameter.DefaultValue
                            : parameterType.IsValueType
                            ? Activator.CreateInstance(parameterType)
                            : null));
                }
            }

            // Create the controller
            bodyContents.Add(Expression.Assign(
                controller,
                buildFactoryExpression(contextInLambda, cancellationTokenInLambda)));

            // Build the handler method call
            var callMethod = Expression.Call(controller, method, handlerArguments);
            if (!typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                // Convert bool to Task<bool>
                callMethod = Expression.Call(TaskFromResultMethod, callMethod);
            }

            // Operations to perform on the controller.
            // Pseudocode:
            //     controller.PreProcessRequest();
            //     return controller.method(handlerArguments);
            Expression workWithController = Expression.Block(
                Expression.Call(controller, PreProcessRequestMethod),
                Expression.Return(returnTarget, callMethod));

            // If the controller type implements IDisposable,
            // wrap operations in a simulated using block.
            if (typeof(IDisposable).IsAssignableFrom(controllerType))
            {
                // Implementation of IDisposable.Dispose
                // IDisposable has only 1 method, so no need to look for the one named "Dispose"
                var disposeMethod = controllerType.GetInterfaceMap(typeof(IDisposable)).TargetMethods[0];

                // Pseudocode:
                //     try
                //     {
                //         body();
                //     }
                //     finally
                //     {
                //         (controller as IDisposable).Dispose();
                //     }
                workWithController = Expression.TryFinally(workWithController, Expression.Call(controller, disposeMethod));
            }

            bodyContents.Add(workWithController);

            // At the end of the lambda body is the target of return statements.
            bodyContents.Add(Expression.Label(returnTarget, Expression.Constant(Task.FromResult(false))));

            // Build and compile the lambda.
            return Expression.Lambda<RouteHandler<IHttpContext>>(
                Expression.Block(locals, bodyContents),
                contextInLambda,
                routeInLambda,
                cancellationTokenInLambda)
                .Compile();
        }

        private Type ValidateControllerType(string argumentName, Type value)
        {
            value = Validate.NotNull(argumentName, value);
            if (value.IsAbstract
             || value.IsGenericTypeDefinition
             || !value.IsSubclassOf(typeof(WebApiController)))
                throw new ArgumentException($"Controller type must be a non-abstract subclass of {nameof(WebApiController)}.", argumentName);

            if (_controllerTypes.Contains(value))
                throw new ArgumentException("Controller type is already registered in this module.", argumentName);

            return value;
        }

        private void RegisterControllerInternal(Type controllerType, Func<Expression, Expression, Expression> buildFactoryExpression)
        {
            var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(m => !m.ContainsGenericParameters
                         && (m.ReturnType == typeof(bool) || m.ReturnType == typeof(Task<bool>)));

            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RouteHandlerAttribute))
                    .OfType<RouteHandlerAttribute>()
                    .ToArray();
                if (attributes.Length < 1)
                    continue;

                foreach (var attribute in attributes)
                {
                    AddHandler(attribute.Verb, attribute.Route, CompileHandler(buildFactoryExpression, method, attribute.Route));
                }
            }

            _controllerTypes.Add(controllerType);
        }
    }
}
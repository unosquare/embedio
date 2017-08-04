namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;

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
                AsyncInvoke = (AsyncDelegate)invokeDelegate;
            else
                SyncInvoke = (SyncDelegate)invokeDelegate;
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

            var argumentExpressions = methodInfo.GetParameters()
                .Select((parameterInfo, i) => Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)), parameterInfo.ParameterType))
                .Cast<Expression>()
                .ToList();

            var callExpression = Expression.Call(Expression.Convert(instanceExpression, methodInfo.DeclaringType),
                methodInfo, argumentExpressions);

            if (isAsync)
                return
                    Expression.Lambda<AsyncDelegate>(Expression.Convert(callExpression, typeof(Task<bool>)),
                        instanceExpression, argumentsExpression).Compile();

            return Expression.Lambda<SyncDelegate>(Expression.Convert(callExpression, typeof(bool)),
                instanceExpression, argumentsExpression).Compile();
        }
    }

    internal class MethodCacheInstance
    {
        public MethodCacheInstance(Func<object> controllerFactory, MethodCache cache)
        {
            ControllerFactory = controllerFactory;
            MethodCache = cache;
        }

        public MethodCache MethodCache { get; }

        public Func<object> ControllerFactory { get; }

        public async Task<bool> Invoke(object[] arguments)
        {
            var controller = ControllerFactory();

            // Now, check if the call is handled asynchronously.
            if (MethodCache.IsTask)
            {
                // Run the method asynchronously
                return await MethodCache.AsyncInvoke(controller, arguments);
            }

            // If the handler is not asynchronous, simply call the method.
            return MethodCache.SyncInvoke(controller, arguments);
        }
    }

    internal class AddtionalParameterInfo
    {
        public AddtionalParameterInfo(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            Converter = TypeDescriptor.GetConverter(parameterInfo.ParameterType);

            if (parameterInfo.ParameterType.GetTypeInfo().IsValueType)
                Default = Activator.CreateInstance(parameterInfo.ParameterType);
        }

        public object Default { get; }
        public ParameterInfo Info { get; }
        public TypeConverter Converter { get; }
    }
}

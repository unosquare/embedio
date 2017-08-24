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
        public MethodCache(MethodInfo methodInfo)
        {
            MethodInfo = methodInfo;
            IsTask = methodInfo.ReturnType == typeof(Task<bool>);
            AdditionalParameters = methodInfo.GetParameters()
                .Skip(2)
                .Select(x => new AddtionalParameterInfo(x))
                .ToList();

            var invokeDelegate = BuildDelegate(methodInfo, IsTask);

            if (IsTask)
                AsyncInvoke = (AsyncDelegate) invokeDelegate;
            else
                SyncInvoke = (SyncDelegate) invokeDelegate;
        }

        public delegate Task<bool> AsyncDelegate(object instance, object[] arguments);

        public delegate bool SyncDelegate(object instance, object[] arguments);

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
                .Select(
                    (parameterInfo, i) =>
                        Expression.Convert(Expression.ArrayIndex(argumentsExpression, Expression.Constant(i)),
                            parameterInfo.ParameterType))
                .Cast<Expression>()
                .ToList();

            var callExpression = Expression.Call(
                Expression.Convert(instanceExpression, methodInfo.DeclaringType),
                methodInfo,
                argumentExpressions);

            if (isAsync)
            {
                return Expression.Lambda<AsyncDelegate>(
                        Expression.Convert(callExpression, typeof(Task<bool>)),
                        instanceExpression,
                        argumentsExpression)
                    .Compile();
            }

            return Expression.Lambda<SyncDelegate>(
                    Expression.Convert(callExpression, typeof(bool)),
                    instanceExpression,
                    argumentsExpression)
                .Compile();
        }
    }

    internal class MethodCacheInstance
    {
        private readonly Func<object> _controllerFactory;

        public MethodCacheInstance(Func<object> controllerFactory, MethodCache cache)
        {
            _controllerFactory = controllerFactory;
            MethodCache = cache;
        }

        public MethodCache MethodCache { get; }

        public void ParseArguments(Dictionary<string, object> parameters, object[] arguments)
        {
            // Parse the arguments to their intended type skipping the first two.
            for (var i = 0; i < MethodCache.AdditionalParameters.Count; i++)
            {
                var param = MethodCache.AdditionalParameters[i];

                // convert and add to arguments, if null use default value
                arguments[i + 2] = parameters.ContainsKey(param.Info.Name)
                    ? param.GetValue((string) parameters[param.Info.Name])
                    : param.Default;
            }
        }

        public Task<bool> Invoke(object[] arguments)
        {
            var controller = _controllerFactory();

            // Now, check if the call is handled asynchronously.
            return MethodCache.IsTask
                ? MethodCache.AsyncInvoke(controller, arguments)
                : Task.FromResult(MethodCache.SyncInvoke(controller, arguments));
        }
    }

    internal class AddtionalParameterInfo
    {
        private readonly TypeConverter _converter;

        public AddtionalParameterInfo(ParameterInfo parameterInfo)
        {
            Info = parameterInfo;
            _converter = TypeDescriptor.GetConverter(parameterInfo.ParameterType);

            if (parameterInfo.ParameterType.GetTypeInfo().IsValueType)
                Default = Activator.CreateInstance(parameterInfo.ParameterType);
        }

        public object Default { get; }
        public ParameterInfo Info { get; }

        public object GetValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                value = null; // ignore whitespace

            // convert and add to arguments, if null use default value
            return value == null ? Default : _converter.ConvertFromString(value);
        }
    }
}
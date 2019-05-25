using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace EmbedIO.WebApi.Internal
{
    internal class MethodCache
    {
        public MethodCache(MethodInfo methodInfo)
        {
            var type = methodInfo?.DeclaringType ?? throw new ArgumentNullException(nameof(methodInfo));

            MethodInfo = methodInfo;
            ControllerName = type.FullName;
            SetHeadersInvoke = ctrl => ctrl.SetDefaultHeaders();
            IsTask = methodInfo.ReturnType == typeof(Task<bool>);
            AdditionalParameters = methodInfo.GetParameters()
                .Select(x => new AdditionalParameterInfo(x))
                .ToList();

            var invokeDelegate = BuildDelegate(methodInfo, IsTask, type);

            if (IsTask)
                AsyncInvoke = (AsyncDelegate) invokeDelegate;
            else
                SyncInvoke = (SyncDelegate) invokeDelegate;
        }

        public delegate Task<bool> AsyncDelegate(object instance, object[] arguments);

        public delegate bool SyncDelegate(object instance, object[] arguments);

        public MethodInfo MethodInfo { get; }
        public Action<WebApiController> SetHeadersInvoke { get; }
        public bool IsTask { get; }
        public List<AdditionalParameterInfo> AdditionalParameters { get; }
        public string ControllerName { get; }
        public AsyncDelegate AsyncInvoke { get; }
        public SyncDelegate SyncInvoke { get; }

        private static Delegate BuildDelegate(MethodInfo methodInfo, bool isAsync, Type type)
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
                Expression.Convert(instanceExpression, type),
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
}
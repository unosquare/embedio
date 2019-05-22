using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Modules;

namespace EmbedIO.Internal
{
    internal class MethodCacheInstance
    {
        private readonly Func<IHttpContext, CancellationToken, object> _controllerFactory;

        public MethodCacheInstance(Func<IHttpContext, CancellationToken, object> controllerFactory, MethodCache cache)
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
                arguments[i] = parameters.ContainsKey(param.Info.Name)
                    ? param.GetValue((string)parameters[param.Info.Name])
                    : param.Default;
            }
        }

        public Task<bool> Invoke(WebApiController controller, object[] arguments) =>
            MethodCache.IsTask
                ? MethodCache.AsyncInvoke(controller, arguments)
                : Task.FromResult(MethodCache.SyncInvoke(controller, arguments));

        public WebApiController SetDefaultHeaders(IHttpContext context, CancellationToken cancellationToken)
        {
            var controller = _controllerFactory(context, cancellationToken) as WebApiController;
            MethodCache.SetHeadersInvoke(controller);

            return controller;
        }
    }
}
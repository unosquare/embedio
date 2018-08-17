namespace Unosquare.Labs.EmbedIO.Tests.Mocks
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;

    internal class TestWebServer : IWebServer
    {
        private readonly WebModules _modules = new WebModules();

        public TestWebServer(RoutingStrategy routingStrategy = RoutingStrategy.Wildcard)
        {
            RoutingStrategy = routingStrategy;
        }

        public ISessionWebModule SessionModule => _modules.SessionModule;
        public RoutingStrategy RoutingStrategy { get; }

        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();
        
        public T Module<T>()
            where T : class, IWebModule
        {
            return _modules.Module<T>();
        }

        public void RegisterModule(IWebModule module) => _modules.RegisterModule(module, this);

        public void UnregisterModule(Type moduleType) => _modules.UnregisterModule(moduleType);

        public Task RunAsync(CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            // do nothing
        }
    }
}

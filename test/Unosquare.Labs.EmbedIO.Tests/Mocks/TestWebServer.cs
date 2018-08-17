namespace Unosquare.Labs.EmbedIO.Tests.Mocks
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;

    public class TestWebServer : IWebServer
    {
        public ISessionWebModule SessionModule { get; }
        public RoutingStrategy RoutingStrategy { get; }
        public ReadOnlyCollection<IWebModule> Modules { get; }

        public T Module<T>() 
            where T : class, IWebModule
        {
            throw new NotImplementedException();
        }

        public void RegisterModule(IWebModule module)
        {
            throw new NotImplementedException();
        }

        public void UnregisterModule(Type moduleType)
        {
            throw new NotImplementedException();
        }

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

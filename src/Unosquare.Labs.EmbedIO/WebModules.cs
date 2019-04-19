namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using Swan;

    internal sealed class WebModules : IDisposable
    {
        private readonly List<IWebModule> _modules = new List<IWebModule>(4);

        ~WebModules()
        {
            Dispose(false);
        }

        public ISessionWebModule SessionModule { get; private set; }

        public ReadOnlyCollection<IWebModule> Modules => _modules.AsReadOnly();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public T Module<T>()
            where T : class, IWebModule
        {
            return Module(typeof(T)) as T;
        }

        public void RegisterModule(IWebModule module, IWebServer webServer)
        {
            if (module == null) return;
            var existingModule = Module(module.GetType());

            if (existingModule == null)
            {
                module.Server = webServer;
                _modules.Add(module);

                if (module is ISessionWebModule webModule)
                    SessionModule = webModule;
            }
            else
            {
                $"Failed to register module '{module.GetType()}' because a module with the same type already exists."
                    .Warn(nameof(WebServer));
            }
        }

        public void UnregisterModule(Type moduleType)
        {
            var existingModule = Module(moduleType);

            if (existingModule == null)
            {
                $"Failed to unregister module '{moduleType}' because no module with that type has been previously registered."
                    .Warn(nameof(WebServer));

                return;
            }

            var module = Module(moduleType);
            _modules.Remove(module);

            if (module is IDisposable disposable)
                disposable.Dispose();

            if (module == SessionModule)
                SessionModule = null;
        }

        public void StartModules(IWebServer webServer, CancellationToken ct)
        {
            foreach (var module in _modules)
            {
                module.Server = webServer;
                module.Start(ct);
            }
        }

        private IWebModule Module(Type moduleType) => _modules.FirstOrDefault(m => m.GetType() == moduleType);

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            foreach (var disposable in _modules.OfType<IDisposable>())
                disposable.Dispose();

            _modules.Clear();
        }
    }
}

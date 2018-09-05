namespace Unosquare.Labs.EmbedIO
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Swan;

    internal class WebModules 
        : List<IWebModule>
    {
        public WebModules() 
            : base(4)
        {
            // Empty
        }
        
        public ISessionWebModule SessionModule { get; protected set; }

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
                Add(module);

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
            Remove(module);

            if (module == SessionModule)
                SessionModule = null;
        }

        private IWebModule Module(Type moduleType) => this.FirstOrDefault(m => m.GetType() == moduleType);
    }
}

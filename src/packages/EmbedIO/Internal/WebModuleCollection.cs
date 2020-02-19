using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Swan.Logging;

namespace EmbedIO.Internal
{
    internal sealed class WebModuleCollection : DisposableComponentCollection<IWebModule>, IComponentCollection<IWebModule>
    {
        private readonly string _logSource;

        private IWebModuleContainer? _container;

        internal WebModuleCollection(string logSource, IWebModuleContainer container)
        {
            _logSource = logSource;
            _container = container;
        }

        /// <inheritdoc />
        public new void Add(string? name, IWebModule component)
        {
            base.Add(name, component);
            component.GetImplementation().SetContainer(_container
                ?? throw new InvalidOperationException($"Cannot add an instance of {component.GetType().Name} to a disposed container."));
        }

        internal void StartAll(CancellationToken cancellationToken)
        {
            foreach (var (name, module) in WithSafeNames)
            {
                $"Starting module {name}...".Debug(_logSource);
                module.Start(cancellationToken);
            }
        }

        internal async Task DispatchRequestAsync(IHttpContext context)
        {
            if (context.IsHandled)
                return;

            var requestedPath = context.RequestedPath;
            foreach (var (name, module) in WithSafeNames)
            {
                var routeMatch = module.MatchUrlPath(requestedPath);
                if (!routeMatch.IsMatch)
                    continue;

                $"[{context.Id}] Processing with {name}.".Debug(_logSource);
                context.GetImplementation().Route = routeMatch;
                await module.HandleRequestAsync(context).ConfigureAwait(false);
                if (context.IsHandled)
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _container = null;
        }
    }
}
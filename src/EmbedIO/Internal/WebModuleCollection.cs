using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Internal
{
    internal class WebModuleCollection : DisposableComponentCollection<IWebModule>
    {
        private readonly string _logSource;

        private readonly string _baseUrlPath;

        internal WebModuleCollection(string logSource, string baseUrlPath)
        {
            _logSource = logSource;
            _baseUrlPath = baseUrlPath;
        }

        internal void StartAll(CancellationToken ct)
        {
            foreach (var (name, module) in this.WithSafeNames)
            {
                $"Starting module {name}...".Debug(_logSource);
                module.Start(ct);
            }
        }

        internal async Task<bool> DispatchRequestAsync(IHttpContext context, CancellationToken ct)
        {
            var requestedPath = UrlPath.UnsafeStripPrefix(context.Request.Url.AbsolutePath, _baseUrlPath);
            if (requestedPath == null)
                return false;

            requestedPath = "/" + requestedPath;

            foreach (var (name, module) in this.WithSafeNames)
            {
                var path = UrlPath.UnsafeStripPrefix(requestedPath, module.BaseUrlPath);
                if (path == null)
                {
                    $"[{context.Id}] Skipping module {name}".Debug(_logSource);
                    continue;
                }

                if (await module.HandleRequestAsync(context, path, ct).ConfigureAwait(false))
                {
                    $"[{context.Id}] Module {name} handled the request.".Info(_logSource);
                    return true;
                }
            }

            $"[{context.Id}] No module handled the request.".Info(_logSource);
            return false;
        }
    }
}
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Internal
{
    internal sealed class WebModuleCollection : DisposableComponentCollection<IWebModule>
    {
        private readonly string _logSource;

        private readonly string _baseUrlPath;

        internal WebModuleCollection(string logSource, string baseUrlPath)
        {
            _logSource = logSource;
            _baseUrlPath = baseUrlPath;
        }

        internal void StartAll(CancellationToken cancellationToken)
        {
            foreach (var (name, module) in this.WithSafeNames)
            {
                $"Starting module {name}...".Debug(_logSource);
                module.Start(cancellationToken);
            }
        }

        internal async Task<bool> DispatchRequestAsync(IHttpContext context, CancellationToken cancellationToken)
        {
            var requestedPath = UrlPath.UnsafeStripPrefix(UrlPath.UnsafeNormalize(context.Request.Url.AbsolutePath, false), _baseUrlPath);
            if (requestedPath == null)
                return false;

            requestedPath = "/" + requestedPath;
            $"[{context.Id}] Requested path = {requestedPath}".Debug(_logSource);

            var contextImpl = context as IHttpContextImpl;
            foreach (var (name, module) in WithSafeNames)
            {
                var mimeTypeProvider = module as IMimeTypeProvider;
                if (mimeTypeProvider != null)
                    contextImpl?.MimeTypeProviders.Push(mimeTypeProvider);

                try
                {
                    var path = UrlPath.UnsafeStripPrefix(requestedPath, module.BaseUrlPath);
                    if (path == null)
                    {
                        $"[{context.Id}] Skipping module {name}".Debug(_logSource);
                        continue;
                    }

                    if (await module.HandleRequestAsync(context, "/" + path, cancellationToken).ConfigureAwait(false))
                    {
                        $"[{context.Id}] Module {name} handled the request.".Info(_logSource);
                        return true;
                    }
                }
                finally
                {
                    if (mimeTypeProvider != null)
                        contextImpl?.MimeTypeProviders.Pop();
                }
            }

            $"[{context.Id}] No module handled the request.".Info(_logSource);
            return false;
        }
    }
}
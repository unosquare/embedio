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

        internal async Task DispatchRequestAsync(IHttpContext context, CancellationToken cancellationToken)
        {
            if (context.Handled)
                return;

            var requestedPath = UrlPath.UnsafeStripPrefix(UrlPath.UnsafeNormalize(context.Request.Url.AbsolutePath, false), _baseUrlPath);
            if (requestedPath == null)
                return;

            requestedPath = "/" + requestedPath;

            var contextImpl = context as IHttpContextImpl;
            foreach (var (name, module) in WithSafeNames)
            {
                var path = UrlPath.UnsafeStripPrefix(requestedPath, module.BaseUrlPath);
                if (path == null)
                    continue;

                var mimeTypeProvider = module as IMimeTypeProvider;
                if (mimeTypeProvider != null)
                    contextImpl?.MimeTypeProviders.Push(mimeTypeProvider);

                try
                {
                    $"[{context.Id}] Processing with {name}.".Debug(_logSource);
                    await module.HandleRequestAsync(context, "/" + path, cancellationToken).ConfigureAwait(false);
                    if (module.IsFinalHandler)
                    {
                        context.Handled = true;
                        break;
                    }
                }
                catch (RequestHandlerPassThroughException)
                {
                    continue;
                }
                finally
                {
                    if (mimeTypeProvider != null)
                        contextImpl?.MimeTypeProviders.Pop();
                }
            }
        }
    }
}
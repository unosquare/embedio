using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO
{
    /// <summary>
    /// Represents our tiny web server used to handle requests.
    ///
    /// This is the default implementation of <c>IWebServer</c> and it's ready to select
    /// the <c>IHttpListener</c> implementation via the proper constructor.
    ///
    /// By default, the WebServer will use the Regex RoutingStrategy for
    /// all registered modules (<c>IWebModule</c>) and EmbedIO Listener (<c>HttpListenerMode</c>).
    /// </summary>
    public abstract class WebServerBase : IWebServer, IDisposable
    {
        private readonly WebModuleCollection _modules = new WebModuleCollection(nameof(WebServerBase), "/");

        private WebServerState _state = WebServerState.Created;

        private ISessionManager _sessionManager;

        /// <summary>
        /// Finalizes an instance of the <see cref="WebServerBase"/> class.
        /// </summary>
        ~WebServerBase()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public event WebServerStateChangedEventHandler StateChanged;

        /// <inheritdoc />
        public IComponentCollection<IWebModule> Modules => _modules;

        /// <inheritdoc />
        public ISessionManager SessionManager
        {
            get; private set;
        }
        
        /// <inheritdoc />
        public WebServerState State
        {
            get => _state;
            protected set
            {
                if (value == _state) return;

                var oldState = _state;
                _state = value;

                if (_state != WebServerState.Created)
                    _modules.Lock();

                StateChanged?.Invoke(this, new WebServerStateChangedEventArgs(oldState, value));
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The method was already called.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        /// <remarks>
        /// Both the server and client requests are queued separately on the thread pool,
        /// so it is safe to call <see cref="Task.Wait()" /> in a synchronous method.
        /// </remarks>
        public async Task RunAsync(CancellationToken ct = default)
        {
            State = WebServerState.Loading;
            Prepare(ct);

            try
            {
                // Init modules
                _modules.StartAll(ct);

                State = WebServerState.Listening;
                await RunInternalAsync(ct).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            finally
            {
                "Cleaning up".Info(GetType().Name);
                State = WebServerState.Stopped;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _modules.Dispose();
        }

        /// <summary>
        /// Prepares a web server for running.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the web server.</param>
        protected virtual void Prepare(CancellationToken ct)
        {
        }

        /// <summary>
        /// Runs the internal request management loop.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the web server.</param>
        /// <returns>A <see cref="Task"/> that is awaited by <see cref="RunAsync"/>.</returns>
        protected abstract Task RunInternalAsync(CancellationToken ct);

        /// <summary>
        /// Handles a client request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous of client request.</returns>
        protected async Task HandleClientRequest(IHttpContext context, CancellationToken ct)
        {
            try
            {
                // Create a request endpoint string
                var requestEndpoint = context.Request.SafeGetRemoteEndpointStr();

                // Log the request and its ID
                $"[{context.Id}] Start: Source {requestEndpoint} - {context.RequestVerb().ToString().ToUpperInvariant()}: {context.Request.Url.PathAndQuery} - {context.Request.UserAgent}"
                    .Debug(nameof(WebServerBase));

                var processResult = await _modules.DispatchRequestAsync(context, ct).ConfigureAwait(false);

                // Return a 404 (Not Found) response if no module/handler handled the response.
                if (processResult == false)
                {
                    $"[{context.Id}] No module generated a response. Sending 404 - Not Found".Error(nameof(WebServerBase));

                    if (OnNotFound == null)
                    {
                        context.Response.StatusCode = 404;
                    }
                    else
                    {
                        await OnNotFound(context).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebServerBase), $"[{context.Id}] Error handling request.");
            }
            finally
            {
                // Always close the response stream no matter what.
                context?.Response.Close();

                $"[{context.Id}] End".Debug(nameof(WebServerBase));
            }
        }

        private async Task<bool> ProcessRequest(IHttpContext context, CancellationToken ct)
        {
            // Iterate though the loaded modules to match up a request and possibly generate a response.
            foreach (var (safeName, module) in _modules.WithSafeNames)
            {
                var callback = GetHandler(context, module);
                if (callback == null) continue;

                try
                {
                    // Log the module and handler to be called and invoke as a callback.
                    $"[{context.Id}] {safeName}::{callback.GetMethodInfo().DeclaringType?.Name}.{callback.GetMethodInfo().Name}"
                        .Debug(nameof(WebServerBase));

                    // Execute the callback
                    var handleResult = await callback(context, ct).ConfigureAwait(false);

                    $"[{context.Id}] Result: {handleResult}".Trace(nameof(WebServerBase));

                    // callbacks can instruct the server to stop bubbling the request through the rest of the modules by returning true;
                    if (handleResult)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions by returning a 500 (Internal Server Error) 
                    if (context.Response.StatusCode != (int)System.Net.HttpStatusCode.Unauthorized)
                    {
                        await ResponseServerError(context, ex, safeName, ct).ConfigureAwait(false);
                    }

                    // Finally set the handled flag to true and exit.
                    return true;
                }
            }

            return false;
        }

        private Task ResponseServerError(IHttpContext context, Exception ex, string module, CancellationToken ct)
        {
            var priorMessage = $"Failing module name: {module}";
            var errorMessage = ex.ExceptionMessage(priorMessage);

            // Log the exception message.
            ex.Log(nameof(WebServerBase), priorMessage);

            // Send the response over with the corresponding status code.
            return context.HtmlResponseAsync(string.Format(CultureInfo.InvariantCulture, Responses.Response500HtmlFormat, errorMessage, ex.StackTrace),
                System.Net.HttpStatusCode.InternalServerError,
                true,
                ct);
        }

        private WebHandler GetHandler(IHttpContext context, IWebModule module)
        {
            Map handler = null;

            void SetHandlerFromRegexPath()
            {
                handler = module.Handlers.FirstOrDefault(x =>
                    (x.Path == ModuleMap.AnyPath || context.RequestRegexUrlParams(x.Path) != null) &&
                    (x.Verb == HttpVerbs.Any || x.Verb == context.RequestVerb()));
            }

            void SetHandlerFromWildcardPath()
            {
                var path = context.RequestWilcardPath(module.Handlers
                    .Where(k => k.Path.Contains(ModuleMap.AnyPathRoute))
                    .Select(s => s.Path.ToLowerInvariant()));

                handler = module.Handlers
                    .FirstOrDefault(x =>
                        (x.Path == ModuleMap.AnyPath || x.Path == path) &&
                        (x.Verb == HttpVerbs.Any || x.Verb == context.RequestVerb()));
            }

            switch (context.WebServer.RoutingStrategy)
            {
                case RoutingStrategy.Wildcard:
                    SetHandlerFromWildcardPath();
                    break;
                case RoutingStrategy.Regex:
                    SetHandlerFromRegexPath();
                    break;
            }

            return handler?.ResponseHandler;
        }

        private void LockConfiguration()
        {
            _modules.Lock();
        }
    }
}
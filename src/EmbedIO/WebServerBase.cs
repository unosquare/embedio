using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO
{
    /// <summary>
    /// Base class for <see cref="IWebServer"/> implementations.
    /// </summary>
    public abstract class WebServerBase : IWebServer, IDisposable
    {
        private readonly WebModuleCollection _modules = new WebModuleCollection(nameof(WebServerBase), "/");

        private WebServerState _state = WebServerState.Created;

        private ISessionManager _sessionManager;

        private bool _configurationLocked;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServerBase"/> class.
        /// </summary>
        protected WebServerBase()
        {
        }

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
            get => _sessionManager;
            set
            {
                if (_configurationLocked)
                    throw new InvalidOperationException($"Cannot set {nameof(SessionManager)} after starting a web server.");

                _sessionManager = value;
            }
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
                {
                    _modules.Lock();
                    _configurationLocked = true;
                }

                StateChanged?.Invoke(this, new WebServerStateChangedEventArgs(oldState, value));
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The method was already called.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public async Task RunAsync(CancellationToken ct = default)
        {
            State = WebServerState.Loading;
            Prepare(ct);

            try
            {
                _sessionManager?.Start(ct);
                _modules.StartAll(ct);

                State = WebServerState.Listening;
                while (!ct.IsCancellationRequested && ShouldProcessMoreRequests())
                {
                    try
                    {
                        var context = await GetContextAsync(ct).ConfigureAwait(false);
                        context.Session = new SessionProxy(context, SessionManager);
                        try
                        {
                            if (ct.IsCancellationRequested)
                                break;

                            // Create a request endpoint string
                            var requestEndpoint = context.Request.SafeGetRemoteEndpointStr();

                            // Log the request and its ID
                            $"[{context.Id}] Start: Source {requestEndpoint} - {context.RequestVerb().ToString().ToUpperInvariant()}: {context.Request.Url.PathAndQuery} - {context.Request.UserAgent}"
                                .Debug(nameof(WebServerBase));

                            try
                            {
                                // Return a 404 (Not Found) response if no module/handler handled the response.
                                if (await _modules.DispatchRequestAsync(context, ct).ConfigureAwait(false))
                                    continue;

                                $"[{context.Id}] No module generated a response. Sending 404 - Not Found".Error(nameof(WebServerBase));
                                context.Response.StandardResponseWithoutBody((int)HttpStatusCode.NotFound);
                            }
                            catch (Exception ex)
                            {
                                ex.Log(nameof(WebServerBase), $"[{context.Id}] Error handling request.");
                                if (context.Response.StatusCode != (int)HttpStatusCode.Unauthorized)
                                {
                                    await context.Response.StandardHtmlResponseAsync(
                                        (int)HttpStatusCode.InternalServerError,
                                        sb => sb
                                            .Append("<h2>Message</h2><pre>")
                                            .Append(ex.ExceptionMessage())
                                            .Append("</pre><h2>Stack Trace</h2><pre>\r\n")
                                            .Append(ex.StackTrace)
                                            .Append("</pre>"),
                                        ct).ConfigureAwait(false);
                                }
                            }
                        }
                        finally
                        {
                            context.Close();
                            $"[{context.Id}] End".Debug(nameof(WebServerBase));
                        }
                    }
                    catch (Exception ex)
                    {
                        OnException();

                        if (ex is OperationCanceledException || ex is ObjectDisposedException || ex is HttpListenerException)
                        {
                            if (!ct.IsCancellationRequested)
                                throw;

                            return;
                        }

                        ex.Log(nameof(WebServerBase));
                    }
                }
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
        /// <para>Tells whether a web server should continue processing requests.</para>
        /// <para>This method is call each time before trying to accept a request.</para>
        /// </summary>
        /// <returns><see langword="true"/> if the web server should continue processing requests;
        /// otherwise, <see langword="false"/>.</returns>
        protected abstract bool ShouldProcessMoreRequests();

        /// <summary>
        /// Asynchronously waits for a request, accepts it, and returns a newly-constructed HTTP context.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the web server.</param>
        /// <returns>An awaitable <see cref="Task"/> that returns a HTTP context.</returns>
        protected abstract Task<IHttpContextImpl> GetContextAsync(CancellationToken ct);

        /// <summary>
        /// <para>Called when an exception is caught in the web server's request processing loop.</para>
        /// <para>This method should tell the server socket to stop accepting further requests.</para>
        /// </summary>
        protected abstract void OnException();
   }
}
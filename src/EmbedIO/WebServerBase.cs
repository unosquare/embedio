﻿using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;
using EmbedIO.Sessions;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO
{
    /// <summary>
    /// Base class for <see cref="IWebServer"/> implementations.
    /// </summary>
    public abstract class WebServerBase : ConfiguredObject, IWebServer, IDisposable
    {
        private readonly WebModuleCollection _modules = new WebModuleCollection(nameof(WebServerBase), "/");

        private WebExceptionHandler _onUnhandledException = StandardExceptionHandlers.Default;

        private WebServerState _state = WebServerState.Created;

        private ISessionManager _sessionManager;

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
        /// <exception cref="InvalidOperationException">The server's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">this property is being set to <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>The default value for this property is <see cref="StandardExceptionHandlers.Default"/>.</para>
        /// </remarks>
        /// <seealso cref="StandardExceptionHandlers"/>
        public WebExceptionHandler OnUnhandledException
        {
            get => _onUnhandledException;
            set
            {
                EnsureConfigurationNotLocked();
                _onUnhandledException = Validate.NotNull(nameof(value), value);
            } 
        }

        /// <inheritdoc />
        public ISessionManager SessionManager
        {
            get => _sessionManager;
            set
            {
                EnsureConfigurationNotLocked();
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
                    LockConfiguration();
                }

                StateChanged?.Invoke(this, new WebServerStateChangedEventArgs(oldState, value));
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The method was already called.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            State = WebServerState.Loading;
            Prepare(cancellationToken);

            try
            {
                _sessionManager?.Start(cancellationToken);
                _modules.StartAll(cancellationToken);

                State = WebServerState.Listening;
                while (!cancellationToken.IsCancellationRequested && ShouldProcessMoreRequests())
                {
                    var context = await GetContextAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable CS4014 // Call is not awaited - of course, it has to run in parallel.
                    Task.Run(() => HandleContextAsync(context, cancellationToken), cancellationToken);
#pragma warning restore CS4014
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                "Operation canceled.".Debug(nameof(WebServerBase));
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

        /// <inheritdoc />
        protected override void OnBeforeLockConfiguration()
        {
            base.OnBeforeLockConfiguration();

            _modules.Lock();
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        protected virtual void Prepare(CancellationToken cancellationToken)
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
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        /// <returns>An awaitable <see cref="Task"/> that returns a HTTP context.</returns>
        protected abstract Task<IHttpContextImpl> GetContextAsync(CancellationToken cancellationToken);

        /// <summary>
        /// <para>Called when an exception is caught in the web server's request processing loop.</para>
        /// <para>This method should tell the server socket to stop accepting further requests.</para>
        /// </summary>
        protected abstract void OnFatalException();

        private async Task HandleContextAsync(IHttpContextImpl context, CancellationToken cancellationToken)
        {
            try
            {
                context.Session = new SessionProxy(context, SessionManager);
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    // Create a request endpoint string
                    var requestEndpoint = context.Request.SafeGetRemoteEndpointStr();

                    // Log the request and its ID
                    $"[{context.Id}] Start: Source {requestEndpoint} - {context.Request.HttpMethod}: {context.Request.Url.PathAndQuery} - {context.Request.UserAgent}"
                        .Debug(nameof(WebServerBase));

                    try
                    {
                        // Return a 404 (Not Found) response if no module handled the response.
                        if (await _modules.DispatchRequestAsync(context, cancellationToken).ConfigureAwait(false))
                            return;

                        $"[{context.Id}] No module generated a response. Sending 404 - Not Found".Error(
                            nameof(WebServerBase));
                        context.Response.SetEmptyResponse((int) HttpStatusCode.NotFound);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw; // Let outer catch block handle it
                    }
                    catch (HttpListenerException)
                    {
                        throw; // Let outer catch block handle it
                    }
                    catch (HttpException ex)
                    {
                        $"[{context.Id}] HttpException: sending status code {ex.StatusCode}".Debug(nameof(WebServerBase));
                        await ex.SendResponseAsync(context).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        ex.Log(nameof(WebServerBase), $"[{context.Id}] Unhandled exception.");
                        context.Response.SetEmptyResponse((int)HttpStatusCode.InternalServerError);
                        context.Response.DisableCaching();
                        await _onUnhandledException(context, context.Request.Url.AbsolutePath, ex, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    context.Close();
                    $"[{context.Id}] End".Debug(nameof(WebServerBase));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                $"[{context.Id}] Operation canceled.".Debug(nameof(WebServerBase));
            }
            catch (HttpListenerException ex)
            {
                ex.Log(nameof(WebServerBase), $"[{context.Id}] Listener exception.");
            }
            catch (Exception ex)
            {
                ex.Log(nameof(WebServerBase), $"[{context.Id}] Fatal exception.");
                OnFatalException();
            }
        }
    }
}
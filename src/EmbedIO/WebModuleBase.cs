using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// <para>Base class to define web modules.</para>
    /// <para>Although it is not required that a module inherits from this class,
    /// it provides some useful features:</para>
    /// <list type="bullet">
    /// <item><description>validation and immutability of the <see cref="BaseUrlPath"/> property,
    /// which are of paramount importance for the correct functioning of a web server;</description></item>
    /// <item><description>support for configuration locking upon web server startup
    /// (see the <see cref="ConfiguredObject.ConfigurationLocked"/> property
    /// and the <see cref="ConfiguredObject.EnsureConfigurationNotLocked"/> method);</description></item>
    /// <item><description>a basic implementation of the <see cref="IWebModule.Start"/> method
    /// for modules that do not need to do anything upon web server startup;</description></item>
    /// <item><description>implementation of the <see cref="OnUnhandledException"/> callback property.</description></item>
    /// </list>
    /// </summary>
    public abstract class WebModuleBase : ConfiguredObject, IWebModule
    {
        private ExceptionHandlerCallback _onUnhandledException;
        private HttpExceptionHandlerCallback _onHttpException;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebModuleBase"/> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path served by this module.</param>
        /// <exception cref="ArgumentNullException"><paramref name="baseUrlPath"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="baseUrlPath"/> is not a valid base URL path.</exception>
        /// <seealso cref="IWebModule.BaseUrlPath"/>
        /// <seealso cref="Validate.UrlPath"/>
        protected WebModuleBase(string baseUrlPath)
        {
            BaseUrlPath = Validate.UrlPath(nameof(baseUrlPath), baseUrlPath, true);
            LogSource = GetType().Name;
        }

        /// <inheritdoc />
        public string BaseUrlPath { get; }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public ExceptionHandlerCallback OnUnhandledException
        {
            get => _onUnhandledException;
            set
            {
                EnsureConfigurationNotLocked();
                _onUnhandledException = value;
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public HttpExceptionHandlerCallback OnHttpException
        {
            get => _onHttpException;
            set
            {
                EnsureConfigurationNotLocked();
                _onHttpException = value;
            }
        }

        /// <inheritdoc />
        public abstract bool IsFinalHandler { get; }

        /// <inheritdoc />
        /// <remarks>
        /// <para>The module's configuration is locked before returning from this method.</para>
        /// </remarks>
        public void Start(CancellationToken cancellationToken)
        {
            OnStart(cancellationToken);
            LockConfiguration();
        }

        /// <inheritdoc />
        public async Task HandleRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            var contextImpl = context as IHttpContextImpl;
            var mimeTypeProvider = this as IMimeTypeProvider;
            if (mimeTypeProvider != null)
                contextImpl?.MimeTypeProviders.Push(mimeTypeProvider);

            try
            {
                await OnRequestAsync(context, path, cancellationToken).ConfigureAwait(false);
                if (IsFinalHandler)
                    context.SetHandled();
            }
            catch (RequestHandlerPassThroughException)
            {
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Let the web server handle it
            }
            catch (HttpListenerException)
            {
                throw; // Let the web server handle it
            }
            catch (Exception exception) when (exception is IHttpException)
            {
                await HttpExceptionHandler.Handle(LogSource, context, exception, _onHttpException, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await ExceptionHandler.Handle(LogSource, context, exception, _onUnhandledException, cancellationToken)
                    .ConfigureAwait(false);

            }
            finally
            {
                if (mimeTypeProvider != null)
                    contextImpl?.MimeTypeProviders.Pop();
            }
        }

        /// <summary>
        /// Gets a string to use as a source for log messages.
        /// </summary>
        protected string LogSource { get; }

        /// <summary>
        /// Called to handle a request from a client.
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="BaseUrlPath"/>. See the Remarks section for more information.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        /// <remarks>
        /// <para>The path specified in the requested URL is stripped of the <see cref="BaseUrlPath"/>
        /// and passed in the <paramref name="path"/> parameter.</para>
        /// <para>The <paramref name="path"/> parameter is in itself a valid URL path, including an initial
        /// slash (<c>/</c>) character.</para>
        /// </remarks>
        protected abstract Task OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken);

        /// <summary>
        /// Called when a module is started, immediately before locking the module's configuration.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        protected virtual void OnStart(CancellationToken cancellationToken)
        {
        }
    }
}
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Internal;
using EmbedIO.Routing;
using EmbedIO.Utilities;
using Swan.Configuration;

namespace EmbedIO
{
    /// <summary>
    /// <para>Base class to define web modules.</para>
    /// <para>Although it is not required that a module inherits from this class,
    /// it provides some useful features:</para>
    /// <list type="bullet">
    /// <item><description>validation and immutability of the <see cref="BaseRoute"/> property,
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
        private readonly RouteMatcher _routeMatcher;
        
        private ExceptionHandlerCallback? _onUnhandledException;
        private HttpExceptionHandlerCallback? _onHttpException;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebModuleBase"/> class.
        /// </summary>
        /// <param name="baseRoute">The base route served by this module.</param>
        /// <exception cref="ArgumentNullException"><paramref name="baseRoute"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="baseRoute"/> is not a valid base route.</exception>
        /// <seealso cref="IWebModule.BaseRoute"/>
        /// <seealso cref="Validate.Route"/>
        protected WebModuleBase(string baseRoute)
        {
            BaseRoute = Validate.Route(nameof(baseRoute), baseRoute, true);
            _routeMatcher = RouteMatcher.Parse(baseRoute, true);
            LogSource = GetType().Name;
        }

        /// <inheritdoc />
        public string BaseRoute { get; }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public ExceptionHandlerCallback? OnUnhandledException
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
        public HttpExceptionHandlerCallback? OnHttpException
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
        
        /// <summary>
        /// Gets a string to use as a source for log messages.
        /// </summary>
        protected string LogSource { get; }

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
        public RouteMatch? MatchUrlPath(string urlPath) => _routeMatcher.Match(urlPath);

        /// <inheritdoc />
        public async Task HandleRequestAsync(IHttpContext context)
        {
            var contextImpl = context.GetImplementation();
            var mimeTypeProvider = this as IMimeTypeProvider;
            if (mimeTypeProvider != null)
                contextImpl?.MimeTypeProviders.Push(mimeTypeProvider);

            try
            {
                await OnRequestAsync(context).ConfigureAwait(false);
                if (IsFinalHandler)
                    context.SetHandled();
            }
            catch (RequestHandlerPassThroughException)
            { }
            catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
            {
                throw; // Let the web server handle it
            }
            catch (HttpListenerException)
            {
                throw; // Let the web server handle it
            }
            catch (Exception exception) when (exception is IHttpException)
            {
                await HttpExceptionHandler.Handle(LogSource, context, exception, _onHttpException)
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await ExceptionHandler.Handle(LogSource, context, exception, _onUnhandledException, _onHttpException)
                    .ConfigureAwait(false);
            }
            finally
            {
                if (mimeTypeProvider != null)
                    contextImpl?.MimeTypeProviders.Pop();
            }
        }

        /// <summary>
        /// Called to handle a request from a client.
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <returns>A <see cref="Task" /> representing the ongoing operation.</returns>
        protected abstract Task OnRequestAsync(IHttpContext context);

        /// <summary>
        /// Called when a module is started, immediately before locking the module's configuration.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        protected virtual void OnStart(CancellationToken cancellationToken)
        {
        }
    }
}
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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
    /// <item><description>support for module-level exception handling via the
    /// <see cref="OnExceptionAsync"/> callback.</description></item>
    /// </list>
    /// </summary>
    public abstract class WebModuleBase : ConfiguredObject, IWebModule
    {
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
        }

        /// <inheritdoc />
        public string BaseUrlPath { get; }

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
        public async Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            try
            {
                return await OnRequestAsync(context, path, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                return await OnExceptionAsync(context, path, e, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Called to handle a request from a client.
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="BaseUrlPath"/>. See the Remarks section for more information.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        /// <remarks>
        /// <para>The path specified in the requested URL is stripped of the <see cref="BaseUrlPath"/>
        /// and passed in the <paramref name="path"/> parameter.</para>
        /// <para>The <paramref name="path"/> parameter is in itself a valid URL path, including an initial
        /// slash (<c>/</c>) character.</para>
        /// </remarks>
        protected abstract Task<bool> OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken);

        /// <summary>
        /// Called when a module is started, immediately before locking the module's configuration.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        protected virtual void OnStart(CancellationToken cancellationToken)
        {
        }

        /// <summary>
        /// <para>Called when an exception is thrown while handling a request.</para>
        /// <para>The default behavior is to rethrow the exception, preserving the original stack trace,
        /// so that it may be handled by the server.</para>
        /// </summary>
        /// <param name="context">The context of the request being handled.</param>
        /// <param name="path">The requested path, relative to <see cref="IWebModule.BaseUrlPath">BaseUrlPath</see>.</param>
        /// <param name="exception">The exception thrown by the controller.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns><see langword="true"/> if the request has been handled;
        /// <see langword="false"/> if the request should be passed down the module chain.</returns>
        protected virtual Task<bool> OnExceptionAsync(IHttpContext context, string path, Exception exception, CancellationToken cancellationToken)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
            return Task.FromResult(false); // Silence warning about not returning a value.
        }
    }
}
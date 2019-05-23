using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Modules;
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
    /// (see the <see cref="ConfigurationLocked"/> property
    /// and the <see cref="EnsureConfigurationNotLocked"/> method);</description></item>
    /// <item><description>a basic implementation of the <see cref="IWebModule.Start"/> method
    /// for modules that do not need to do anything upon web server startup.</description></item>
    /// </list>
    /// </summary>
    public abstract class WebModuleBase : IWebModule
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
        public void Start(CancellationToken ct)
        {
            OnStart(ct);
            LockConfiguration();
        }

        /// <inheritdoc />
        public abstract Task<bool> HandleRequestAsync(IHttpContext context, string path, CancellationToken ct);

        /// <summary>
        /// Called when a module is started, immediately before locking the module's configuration.
        /// </summary>
        /// <param name="ct">A <see cref="CancellationToken"/> used to stop the web server.</param>
        protected virtual void OnStart(CancellationToken ct)
        {
        }

        /// <summary>
        /// Gets a value indicating whether a module's configuration has already been locked
        /// and has therefore become read-only.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the configuration is locked; otherwise, <see langword="false"/>.
        /// </value>
        /// <seealso cref="EnsureConfigurationNotLocked"/>
        protected bool ConfigurationLocked { get; private set; }

        /// <summary>
        /// <para>Locks the module's configuration, preventing further modifications.</para>
        /// </summary>
        /// <remarks>
        /// <para>Configuration locking must be enforced by derived classes
        /// by calling <see cref="EnsureConfigurationNotLocked"/> at the start
        /// of methods and property setters that could change the module's
        /// configuration.</para>
        /// <para>This method may be called at any time, for example to prevent adding further controllers
        /// to a <see cref="WebApiModule"/>-derived class; however, it is not
        /// necessary to call it in all cases, because <see cref="WebModuleBase"/> calls it automatically
        /// immediately after the <see cref="OnStart"/> method returns.</para>
        /// </remarks>
        protected void LockConfiguration() => ConfigurationLocked = true;

        /// <summary>
        /// Checks whether a module's configuration has become read-only
        /// and, if so, throws an <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configuration is locked.</exception>
        /// <seealso cref="ConfigurationLocked"/>
        protected void EnsureConfigurationNotLocked()
        {
            if (ConfigurationLocked)
                throw new InvalidOperationException("Cannot configure a module once it has been started.");
        }
    }
}
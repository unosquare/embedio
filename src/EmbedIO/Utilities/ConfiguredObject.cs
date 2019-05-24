using System;
using EmbedIO.Modules;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Base class for objects whose configuration may be locked,
    /// thus becoming read-only, at a certain moment in their lifetime.
    /// </summary>
    public abstract class ConfiguredObject
    {
        /// <summary>
        /// Gets a value indicating whether s configuration has already been locked
        /// and has therefore become read-only.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the configuration is locked; otherwise, <see langword="false"/>.
        /// </value>
        /// <seealso cref="EnsureConfigurationNotLocked"/>
        protected bool ConfigurationLocked { get; private set; }

        /// <summary>
        /// <para>Locks this instance's configuration, preventing further modifications.</para>
        /// </summary>
        /// <remarks>
        /// <para>Configuration locking must be enforced by derived classes
        /// by calling <see cref="EnsureConfigurationNotLocked"/> at the start
        /// of methods and property setters that could change the object's
        /// configuration.</para>
        /// <para>This method may be called at any time, for example to prevent adding further controllers
        /// to a <see cref="WebApiModule"/>-derived class.</para>
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
                throw new InvalidOperationException($"Configuration of this {GetType().Name} instance is locked.");
        }
    }
}
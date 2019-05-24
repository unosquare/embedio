using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Base class for objects whose configuration may be locked,
    /// thus becoming read-only, at a certain moment in their lifetime.
    /// </summary>
    public abstract class ConfiguredObject
    {
        private readonly object _syncRoot = new object();
        private bool _configurationLocked;

        /// <summary>
        /// Gets a value indicating whether s configuration has already been locked
        /// and has therefore become read-only.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the configuration is locked; otherwise, <see langword="false"/>.
        /// </value>
        /// <seealso cref="EnsureConfigurationNotLocked"/>
        protected bool ConfigurationLocked
        {
            get
            {
                lock (_syncRoot)
                {
                    return _configurationLocked;
                }
            }
        }

        /// <summary>
        /// <para>Locks this instance's configuration, preventing further modifications.</para>
        /// </summary>
        /// <remarks>
        /// <para>Configuration locking must be enforced by derived classes
        /// by calling <see cref="EnsureConfigurationNotLocked"/> at the start
        /// of methods and property setters that could change the object's
        /// configuration.</para>
        /// <para>Immediately before locking the configuration, this method calls <see cref="OnBeforeLockConfiguration"/>
        /// as a last chance to validate configuration data, and to lock the configuration of contained objects.</para>
        /// </remarks>
        /// <seealso cref="OnBeforeLockConfiguration"/>
        protected void LockConfiguration()
        {
            lock (_syncRoot)
            {
                if (_configurationLocked)
                    return;

                OnBeforeLockConfiguration();
                _configurationLocked = true;
            }
        }

        /// <summary>
        /// Called immediately before locking the configuration.
        /// </summary>
        /// <seealso cref="LockConfiguration"/>
        protected virtual void OnBeforeLockConfiguration()
        {
        }

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
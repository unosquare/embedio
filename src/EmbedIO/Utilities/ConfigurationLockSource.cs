using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Signals to a <see cref="ConfigurationLockToken"/> that an object's configuration is locked.
    /// </summary>
    /// <remarks>
    /// <para><see cref="ConfigurationLockSource"/> is used to instantiate a <see cref="ConfigurationLockToken"/>
    /// (via the source's <see cref="Token">Token</see> property) that can be handed to owned objects
    /// that wish to be notified of configuration locking or that can be used to
    /// register operations to be performed upon locking.</para>
    /// <para>All members of this class are thread-safe and may be used
    /// concurrently from multiple threads.</para>
    /// </remarks>
    /// <seealso cref="ConfigurationLockToken"/>
    [ComVisible(false)]

    [DebuggerDisplay("IsLocked = {IsLocked}")]
    public class ConfigurationLockSource
    {
        #region Private data

        readonly string? _ownerDescription;

        volatile bool _isLocked;

        #endregion

        #region Instance management

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="ConfigurationLockSource"/> class.</para>
        /// <para>The <see cref="Exception.Message">Message</see> property of exceptions thrown
        /// by the <see cref="ConfigurationLockToken.ThrowIfLocked">ThrowIfLocked</see> method
        /// of tokens associated with this instance will be a generic message, with no clue
        /// as to which object's configuration is locked.</para>
        /// </summary>
        public ConfigurationLockSource()
        {
            _ownerDescription = null;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="ConfigurationLockSource"/> class.</para>
        /// <para>The <see cref="Exception.Message">Message</see> property of exceptions thrown
        /// by the <see cref="ConfigurationLockToken.ThrowIfLocked">ThrowIfLocked</see> method
        /// of tokens associated with this instance will mention the type of the object
        /// whose configuration is locked, including namespaces.</para>
        /// </summary>
        /// <param name="ownerType">The type of the object owning the configuration.</param>
        public ConfigurationLockSource(Type ownerType)
        {
            _ownerDescription = $"an instance of {Validate.NotNull(nameof(ownerType), ownerType).FullName}";
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="ConfigurationLockSource"/> class.</para>
        /// <para>The <see cref="Exception.Message">Message</see> property of exceptions thrown
        /// by the <see cref="ConfigurationLockToken.ThrowIfLocked">ThrowIfLocked</see> method
        /// of tokens associated with this instance will include the specified description,
        /// in the following fashion: <c>"The configuration of {ownerDescription} is locked and cannot be further changed."</c>.</para>
        /// </summary>
        /// <param name="ownerDescription">A brief description of the object owning the configuration.</param>
        public ConfigurationLockSource(string ownerDescription)
        {
            _ownerDescription = Validate.NotNullOrEmpty(nameof(ownerDescription), ownerDescription);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether this <see cref="ConfigurationLockSource">ConfigurationLockSource</see>
        /// has been locked.
        /// </summary>
        public bool IsLocked => _isLocked;

        /// <summary>
        /// Gets a <see cref="ConfigurationLockToken">ConfigurationLockToken</see>
        /// associated with this <see cref="ConfigurationLockSource"/>.
        /// </summary>
        public ConfigurationLockToken Token => new ConfigurationLockToken(this);

        /// <summary>
        /// Communicates that configuration is locked.
        /// </summary>
        public void Lock() => _isLocked = true;

        #endregion

        #region Internal API

        internal void ThrowIfLocked()
        {
            if (IsLocked)
            {
                throw _ownerDescription == null
                    ? new ConfigurationLockedException(Token)
                    : new ConfigurationLockedException($"The configuration of {_ownerDescription} is locked and cannot be further changed.", Token);
            }
        }

        #endregion
    }
}
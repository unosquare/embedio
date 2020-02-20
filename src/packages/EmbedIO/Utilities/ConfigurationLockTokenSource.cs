using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Signals to associated instances of <see cref="LockToken"/> that an object's configuration is locked.
    /// </summary>
    /// <seealso cref="LockTokenSource"/>
    /// <seealso cref="LockToken"/>
    [ComVisible(false)]

    [DebuggerDisplay("IsLocked = {IsLocked}")]
    public class ConfigurationLockSource : LockTokenSource
    {
        #region Instance management

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="ConfigurationLockSource"/> class.</para>
        /// <para>The <see cref="Exception.Message">Message</see> property of exceptions thrown
        /// by the <see cref="LockToken.ThrowIfLocked">ThrowIfLocked</see> method
        /// of tokens associated with this instance will mention the type of the object
        /// whose configuration is locked, including namespaces.</para>
        /// </summary>
        /// <param name="ownerType">The type of the object owning the configuration.</param>
        public ConfigurationLockSource(Type ownerType)
            : base($"The configuration of an instance of {Validate.NotNull(nameof(ownerType), ownerType).FullName} is locked and cannot be further changed.")
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="ConfigurationLockSource"/> class.</para>
        /// <para>The <see cref="Exception.Message">Message</see> property of exceptions thrown
        /// by the <see cref="LockToken.ThrowIfLocked">ThrowIfLocked</see> method
        /// of tokens associated with this instance will include the specified description,
        /// in the following fashion: <c>"The configuration of {ownerDescription} is locked and cannot be further changed."</c>.</para>
        /// </summary>
        /// <param name="ownerDescription">A brief description of the object owning the configuration.</param>
        public ConfigurationLockSource(string ownerDescription)
            : base($"The configuration of {Validate.NotNullOrEmpty(nameof(ownerDescription), ownerDescription)} is locked and cannot be further changed.")
        {
        }

        #endregion
    }
}
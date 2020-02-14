using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Propagates notification that the configuration of an object has been locked.
    /// </summary>
    /// <remarks>
    /// <para>A <see cref="ConfigurationLockToken"/> is usually created through the
    /// <see cref="ConfigurationLockSource.Token">Token</see> property of an instance of
    /// <see cref="ConfigurationLockSource"/>.</para>
    /// <para>Once locked, a token will never transition to a non-locked state.</para>
    /// <para>The special token named <see cref="None"/>, equal to
    /// <c><see langword="default"/>(ConfigurationLockToken)</c>, is the only token
    /// that can never be locked.</para>
    /// <para>All members of this <see langword="struct"/> are thread-safe
    /// and may be used concurrently from multiple threads.</para>
    /// </remarks>
    /// <seealso cref="ConfigurationLockSource"/>
    [ComVisible(false)]
    [DebuggerDisplay("IsLocked = {IsLocked}")]
    public struct ConfigurationLockToken : IEquatable<ConfigurationLockToken>
    {
        #region Private data

        readonly ConfigurationLockSource? _source;

        #endregion

        #region Instance management

        internal ConfigurationLockToken(ConfigurationLockSource source)
        {
            _source = Validate.NotNull(nameof(source), source);
        }

        #endregion

        #region Public API

        /// <summary>
        /// <para>Gets an empty <see cref="ConfigurationLockToken"/>,
        /// i.e. a token that has no source and will never be locked.</para>
        /// <para>The value of this property is equal to
        /// <c><see langword="default"/>(ConfigurationLockToken)</c>.</para>
        /// </summary>
        public static ConfigurationLockToken None { get; } = default;

        /// <summary>
        /// Gets whether configuration is locked for this token.
        /// </summary>
        public bool IsLocked => _source?.IsLocked ?? false;

        /// <summary>
        /// Throws a <see cref="ConfigurationLockedException"/> if the configuration for this token is locked.
        /// </summary>
        /// <exception cref="ConfigurationLockedException">The configuration is locked.</exception>
        public void ThrowIfLocked() => _source?.ThrowIfLocked();

        #endregion

        #region Overrides of Object

        /// <inheritdoc />
        public override int GetHashCode() => _source == null ? 0 : _source.GetHashCode();

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ConfigurationLockToken other && Equals(other);

        #endregion

        #region Implementation of IEquatable<ConfigurationLockToken>

        /// <summary>
        /// Determines whether the current <see cref="ConfigurationLockToken"/> instance
        /// is equal to the specified token.
        /// </summary>
        /// <param name="other">The other <see cref="ConfigurationLockToken"/> to compare with this instance.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.
        /// See the Remarks section for more information.</returns>
        /// <remarks>
        /// <para>Two configuration lock tokens are equal if any one of the following conditions is true:</para>
        /// <list type="bullet">
        ///   <item><term>they are associated with the same <see cref="ConfigurationLockSource"/>;</term></item>
        ///   <item><term>the value of both tokens is <see cref="None"/>.</term></item>
        /// </list>
        /// </remarks>
        public bool Equals(ConfigurationLockToken other) => _source != null && _source == other._source;

        #endregion

        #region Operators

        /// <summary>
        /// Determines whether two <see cref="ConfigurationLockToken"/> instances are equal.
        /// </summary>
        /// <param name="a">The first instance.</param>
        /// <param name="b">The second instance.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.
        /// For the definition of equality, see the <see cref="Equals(ConfigurationLockToken)">Equals</see> method.</returns>
        /// <seealso cref="operator!="/>
        public static bool operator ==(ConfigurationLockToken a, ConfigurationLockToken b) => a.Equals(b);

        /// <summary>
        /// Determines whether two <see cref="ConfigurationLockToken"/> instances are not equal.
        /// </summary>
        /// <param name="a">The first instance.</param>
        /// <param name="b">The second instance.</param>
        /// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.
        /// For the definition of equality, see the <see cref="Equals(ConfigurationLockToken)">Equals</see> method.</returns>
        /// <seealso cref="operator=="/>
        public static bool operator !=(ConfigurationLockToken a, ConfigurationLockToken b) => !a.Equals(b);

        #endregion
    }
}
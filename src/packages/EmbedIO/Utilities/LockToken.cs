using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Propagates notification that some data has been locked and can no longer be modified.
    /// </summary>
    /// <remarks>
    /// <para>A <see cref="LockToken"/> is usually created through the
    /// <see cref="LockTokenSource.Token">Token</see> property of an instance of
    /// <see cref="LockTokenSource"/>.</para>
    /// <para>Once locked, a token will never transition to a non-locked state.</para>
    /// <para>The special token named <see cref="None"/>, equal to
    /// <c><see langword="default"/>(LockToken)</c>, is the only token
    /// that can never be locked.</para>
    /// <para>All members of this <see langword="struct"/> are thread-safe
    /// and may be used concurrently from multiple threads.</para>
    /// </remarks>
    /// <seealso cref="LockTokenSource"/>
    [ComVisible(false)]
    [DebuggerDisplay("IsLocked = {IsLocked}")]
    public struct LockToken : IEquatable<LockToken>
    {
        #region Private data

        readonly LockTokenSource? _source;

        #endregion

        #region Instance management

        internal LockToken(LockTokenSource source)
        {
            _source = Validate.NotNull(nameof(source), source);
        }

        #endregion

        #region Public API

        /// <summary>
        /// <para>Gets an empty <see cref="LockToken"/>,
        /// i.e. a token that has no source and will never be locked.</para>
        /// <para>The value of this property is equal to
        /// <c><see langword="default"/>(LockToken)</c>.</para>
        /// </summary>
        public static LockToken None { get; } = default;

        /// <summary>
        /// Gets whether this token is locked.
        /// </summary>
        public bool IsLocked => _source?.IsLocked ?? false;

        /// <summary>
        /// Throws a <see cref="LockedException"/> if this token is locked.
        /// </summary>
        /// <exception cref="LockedException">The token is locked.</exception>
        public void ThrowIfLocked() => _source?.ThrowIfLocked();

        #endregion

        #region Overrides of Object

        /// <inheritdoc />
        public override int GetHashCode() => _source == null ? 0 : _source.GetHashCode();

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is LockToken other && Equals(other);

        #endregion

        #region Implementation of IEquatable<LockToken>

        /// <summary>
        /// Determines whether the current <see cref="LockToken"/> instance
        /// is equal to the specified token.
        /// </summary>
        /// <param name="other">The other <see cref="LockToken"/> to compare with this instance.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.
        /// See the Remarks section for more information.</returns>
        /// <remarks>
        /// <para>Two lock tokens are equal if any one of the following conditions is true:</para>
        /// <list type="bullet">
        ///   <item><term>they are associated with the same <see cref="LockTokenSource"/>;</term></item>
        ///   <item><term>the value of both tokens is <see cref="None"/>.</term></item>
        /// </list>
        /// </remarks>
        public bool Equals(LockToken other) => _source != null && _source == other._source;

        #endregion

        #region Operators

        /// <summary>
        /// Determines whether two <see cref="LockToken"/> instances are equal.
        /// </summary>
        /// <param name="a">The first instance.</param>
        /// <param name="b">The second instance.</param>
        /// <returns><see langword="true"/> if the instances are equal; otherwise, <see langword="false"/>.
        /// For the definition of equality, see the <see cref="Equals(LockToken)">Equals</see> method.</returns>
        /// <seealso cref="operator!="/>
        public static bool operator ==(LockToken a, LockToken b) => a.Equals(b);

        /// <summary>
        /// Determines whether two <see cref="LockToken"/> instances are not equal.
        /// </summary>
        /// <param name="a">The first instance.</param>
        /// <param name="b">The second instance.</param>
        /// <returns><see langword="true"/> if the instances are not equal; otherwise, <see langword="false"/>.
        /// For the definition of equality, see the <see cref="Equals(LockToken)">Equals</see> method.</returns>
        /// <seealso cref="operator=="/>
        public static bool operator !=(LockToken a, LockToken b) => !a.Equals(b);

        #endregion
    }
}
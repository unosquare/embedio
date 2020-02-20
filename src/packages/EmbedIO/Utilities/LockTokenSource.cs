using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// Signals to associated instances of <see cref="LockToken"/> that some data is locked.
    /// </summary>
    /// <remarks>
    /// <para><see cref="LockTokenSource"/> is used to instantiate a <see cref="LockToken"/>
    /// (via the source's <see cref="Token">Token</see> property) that can be handed to owned objects
    /// that wish to be notified of data locking.</para>
    /// <para>All members of this class are thread-safe and may be used
    /// concurrently from multiple threads.</para>
    /// </remarks>
    /// <seealso cref="LockToken"/>
    [ComVisible(false)]

    [DebuggerDisplay("IsLocked = {IsLocked}")]
    public class LockTokenSource
    {
        #region Private data

        readonly string _exceptionMessage;

        volatile bool _isLocked;

        #endregion

        #region Instance management

        /// <summary>
        /// <para>Initializes a new instance of the <see cref="LockTokenSource"/> class.</para>
        /// <para>The <see cref="Exception.Message">Message</see> property of exceptions thrown
        ///  will include the specified description,
        /// in the following fashion: <c>"The configuration of {ownerDescription} is locked and cannot be further changed."</c>.</para>
        /// </summary>
        /// <param name="exceptionMessage">The message to set on instances of <see cref="LockedException"/>
        /// thrown by the <see cref="LockToken.ThrowIfLocked">ThrowIfLocked</see> method of tokens
        /// associated with this instance.</param>
        public LockTokenSource(string exceptionMessage)
        {
            _exceptionMessage = Validate.NotNullOrEmpty(nameof(exceptionMessage), exceptionMessage);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets whether this <see cref="LockTokenSource">LockTokenSource</see>
        /// has been locked.
        /// </summary>
        public bool IsLocked => _isLocked;

        /// <summary>
        /// Gets a <see cref="LockToken">LockToken</see>
        /// associated with this <see cref="LockTokenSource"/>.
        /// </summary>
        public LockToken Token => new LockToken(this);

        /// <summary>
        /// Communicates that data is locked.
        /// </summary>
        public void Lock() => _isLocked = true;

        #endregion

        #region Internal API

        internal void ThrowIfLocked()
        {
            if (IsLocked)
                throw new LockedException(_exceptionMessage, Token);
        }

        #endregion
    }
}
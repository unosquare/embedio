using System;
using System.Collections.Generic;

namespace EmbedIO.Sessions
{
    /// <summary>
    /// Provides useful constants related to session management.
    /// </summary>
    public static class Session
    {
        /// <summary>
        /// <para>The <seealso cref="StringComparison"/> used to disambiguate session IDs.</para>
        /// <para>Corresponds to <see cref="StringComparison.Ordinal"/>.</para>
        /// </summary>
        public const StringComparison IdComparison = StringComparison.Ordinal;

        /// <summary>
        /// <para>The <seealso cref="StringComparison"/> used to disambiguate session keys.</para>
        /// <para>Corresponds to <see cref="StringComparison.InvariantCulture"/>.</para>
        /// </summary>
        public const StringComparison KeyComparison = StringComparison.InvariantCulture;

        /// <summary>
        /// <para>The equality comparer used for session IDs.</para>
        /// <para>Corresponds to <see cref="StringComparer.Ordinal"/>.</para>
        /// </summary>
        public static readonly IEqualityComparer<string> IdComparer = StringComparer.Ordinal;

        /// <summary>
        /// <para>The equality comparer used for session keys.</para>
        /// <para>Corresponds to <see cref="StringComparer.InvariantCulture"/>.</para>
        /// </summary>
        public static readonly IEqualityComparer<string> KeyComparer = StringComparer.InvariantCulture;
    }
}
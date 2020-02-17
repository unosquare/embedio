using System;

namespace EmbedIO.Utilities
{
    /// <summary>
    /// <para>Generates locally unique string IDs, mainly for logging purposes.</para>
    /// </summary>
    public static class UniqueIdGenerator
    {
        /// <summary>
        /// Generates and returns a unique ID.
        /// </summary>
        /// <returns>The generated ID.</returns>
        public static string GetNext() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 22);
    }
}
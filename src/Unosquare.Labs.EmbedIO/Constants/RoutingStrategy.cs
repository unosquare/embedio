namespace Unosquare.Labs.EmbedIO.Constants
{
    using System;

    /// <summary>
    /// Defines the routing strategy for URL matching
    /// This is especially useful for REST service implementations
    /// in the WebApi module.
    /// </summary>
    public enum RoutingStrategy
    {
        /// <summary>
        /// The wildcard strategy
        /// </summary>
        [Obsolete("Wilcard routing will be dropped in future versions")]
        Wildcard,

        /// <summary>
        /// The Regex strategy, default one
        /// </summary>
        Regex,
    }
}

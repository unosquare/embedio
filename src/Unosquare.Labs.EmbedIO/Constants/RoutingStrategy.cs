namespace Unosquare.Labs.EmbedIO.Constants
{
    /// <summary>
    /// Defines the routing strategy for URL matching
    /// This is especially useful for REST service implementations
    /// in the WebApi module.
    /// </summary>
    public enum RoutingStrategy
    {
        /// <summary>
        /// The wildcard strategy, default one
        /// </summary>
        Wildcard,

        /// <summary>
        /// The Regex strategy
        /// </summary>
        Regex,

        /// <summary>
        /// The simple strategy
        /// </summary>
        Simple,
    }
}

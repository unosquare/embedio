namespace EmbedIO.Constants
{
    /// <summary>
    /// Defines the routing strategy for URL matching
    /// This is especially useful for REST service implementations
    /// in the WebApi module.
    /// </summary>
    public enum RoutingStrategy
    {
        /// <summary>
        /// The Regex strategy (default)
        /// </summary>
        Regex,

        /// <summary>
        /// The wildcard strategy
        /// </summary>
        Wildcard,
    }
}

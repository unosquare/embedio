namespace EmbedIO.Modules
{
    /// <summary>
    /// Defines the routing strategies used by <see cref="WebApiModule"/>
    /// for matching URLs to controllers.
    /// </summary>
    public enum WebApiRoutingStrategy
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
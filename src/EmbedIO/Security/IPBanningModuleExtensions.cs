namespace EmbedIO.Security
{
    /// <summary>
    /// Provides extension methods for <see cref="IPBanningModule"/> and derived classes.
    /// </summary>
    public static class IPBanningModuleExtensions
    {
        /// <summary>
        /// Adds a collection of valid IPs that never will be banned.
        /// </summary>
        /// <typeparam name="TModule">The type of the module.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">A collection of valid IPs that never will be banned.</param>
        /// <returns>
        ///     <paramref name="this"/> with its whitelist configured.
        /// </returns>
        public static TModule WithWhitelist<TModule>(this TModule @this, params string[] value)
            where TModule : IPBanningModule
        {
            @this.AddToWhitelist(value);
            return @this;
        }

        /// <summary>
        /// Add a collection of Regex to match the log messages against as a criterion for banning IP addresses.
        /// </summary>
        /// <typeparam name="TModule">The type of the module.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">A collection of regex to match log messages against.</param>
        /// <returns>
        ///     <paramref name="this"/> with a fail regex criterion configured.
        /// </returns>
        public static TModule WithRegexRules<TModule>(this TModule @this, params string[] value)
            where TModule : IPBanningModule =>
        WithRegexRules(@this, IPBanningRegexCriterion.DefaultMaxMatchCount, IPBanningRegexCriterion.DefaultSecondsMatchingPeriod, value);

        /// <summary>
        /// Add a collection of Regex to match the log messages against as a criterion for banning IP addresses.
        /// </summary>
        /// <typeparam name="TModule">The type of the module.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="maxMatchCount">The maximum match count.</param>
        /// <param name="secondsMatchingPeriod">The seconds matching period.</param>
        /// <param name="value">A collection of regex to match log messages against.</param>
        /// <returns>
        ///   <paramref name="this" /> with a fail regex criterion configured.
        /// </returns>
        public static TModule WithRegexRules<TModule>(this TModule @this,
            int maxMatchCount, 
            int secondsMatchingPeriod,
            params string[] value)
            where TModule : IPBanningModule
        {
            @this.RegisterCriterion(new IPBanningRegexCriterion(@this, value, maxMatchCount, secondsMatchingPeriod));
            return @this;
        }

        /// <summary>
        /// Sets a maximum amount of requests per second as a criterion for banning IP addresses.
        /// </summary>
        /// <typeparam name="TModule">The type of the module.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="maxRequests">The maximum requests per second.</param>
        /// <returns>
        ///     <paramref name="this"/> with a maximum requests per second configured.
        /// </returns>
        public static TModule WithMaxRequestsPerSecond<TModule>(this TModule @this, int maxRequests = IPBanningRequestsCriterion.DefaultMaxRequestsPerSecond)
            where TModule : IPBanningModule
        {
            @this.RegisterCriterion(new IPBanningRequestsCriterion(maxRequests));
            return @this;
        }
    }
}

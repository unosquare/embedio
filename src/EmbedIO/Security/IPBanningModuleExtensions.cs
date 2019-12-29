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
        /// Add a collection of Regex to match the log messages against.
        /// </summary>
        /// <typeparam name="TModule">The type of the module.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">A collection of regex to match log messages against.</param>
        /// <returns>
        ///     <paramref name="this"/> with its fail regex configured.
        /// </returns>
        public static TModule WithRegexRules<TModule>(this TModule @this, params string[] value)
            where TModule : IPBanningModule
        {
            @this.RegisterCriterion(new IPBanningRegexCriterion(@this, value));
            return @this;
        }

        public static TModule WithMaxRequestsPerSecond<TModule>(this TModule @this, int maxRequests = IPBanningRequestsCriterion.DefaultMaxRequestsPerSecond)
            where TModule : IPBanningModule
        {
            @this.RegisterCriterion(new IPBanningRequestsCriterion(maxRequests));
            return @this;
        }
    }
}

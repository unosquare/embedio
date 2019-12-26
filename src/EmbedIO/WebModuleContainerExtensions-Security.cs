using EmbedIO.Security;
using System;
using System.Collections.Generic;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="IPBanningModule" /> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer" /> on which this method is called.</param>
        /// <param name="configure">A callback used to configure the module.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        /// <param name="maxRequestPerSecond">The maximum requests per second.</param>
        /// <param name="maxMatchCount">The maximum number of regex matches before banning an IP.</param>
        /// <param name="secondsMatchingPeriod">The matching period.</param>
        /// <returns>
        ///   <paramref name="this" /> with a <see cref="IPBanningModule" /> added.
        /// </returns>
        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            Action<IPBanningModule>? configure = null,
            int banMinutes = IPBanningModule.DefaultBanMinutes,
            int maxRequestPerSecond = IPBanningModule.DefaultMaxRequestsPerSecond,
            int maxMatchCount = IPBanningModule.DefaultMaxMatchCount,
            int secondsMatchingPeriod = IPBanningModule.DefaultSecondsMatchingPeriod)
            where TContainer : class, IWebModuleContainer =>
            SetModule(@this, null, null, banMinutes, maxRequestPerSecond, maxMatchCount, secondsMatchingPeriod, configure);

        /// <summary>
        /// Creates an instance of <see cref="IPBanningModule" /> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer" /> on which this method is called.</param>
        /// <param name="failRegex">A collection of regex to match log messages against.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        /// <param name="maxRequestPerSecond">The maximum requests per second.</param>
        /// <param name="maxMatchCount">The maximum number of regex matches before banning an IP.</param>
        /// <param name="secondsMatchingPeriod">The matching period.</param>
        /// <returns>
        ///   <paramref name="this" /> with a <see cref="IPBanningModule" /> added.
        /// </returns>
        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string> failRegex,
            int banMinutes = IPBanningModule.DefaultBanMinutes,
            int maxRequestPerSecond = IPBanningModule.DefaultMaxRequestsPerSecond,
            int maxMatchCount = IPBanningModule.DefaultMaxMatchCount,
            int secondsMatchingPeriod = IPBanningModule.DefaultSecondsMatchingPeriod)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, null, banMinutes, maxRequestPerSecond, maxMatchCount, secondsMatchingPeriod);

        /// <summary>
        /// Creates an instance of <see cref="IPBanningModule" /> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer" /> on which this method is called.</param>
        /// <param name="failRegex">A collection of regex to match log messages against.</param>
        /// <param name="whitelist">A collection of valid IPs that never will be banned.</param>
        /// <param name="banMinutes">Minutes that an IP will remain banned.</param>
        /// <param name="maxRequestPerSecond">The maximum requests per second.</param>
        /// <param name="maxMatchCount">The maximum number of regex matches before banning an IP.</param>
        /// <param name="secondsMatchingPeriod">The matching period.</param>
        /// <returns>
        ///   <paramref name="this" /> with a <see cref="IPBanningModule" /> added.
        /// </returns>
        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string>? failRegex,
            IEnumerable<string>? whitelist = null,
            int banMinutes = IPBanningModule.DefaultBanMinutes,
            int maxRequestPerSecond = IPBanningModule.DefaultMaxRequestsPerSecond,
            int maxMatchCount = IPBanningModule.DefaultMaxMatchCount,
            int secondsMatchingPeriod = IPBanningModule.DefaultSecondsMatchingPeriod)
            where TContainer : class, IWebModuleContainer =>
            SetModule(@this, failRegex, whitelist, banMinutes, maxRequestPerSecond, maxMatchCount, secondsMatchingPeriod);

        private static TContainer SetModule<TContainer>(this TContainer @this,
            IEnumerable<string>? failRegex,
            IEnumerable<string>? whitelist = null,
            int banMinutes = IPBanningModule.DefaultBanMinutes,
            int maxRequestPerSecond = IPBanningModule.DefaultMaxRequestsPerSecond,
            int maxMatchCount = IPBanningModule.DefaultMaxMatchCount,
            int secondsMatchingPeriod = IPBanningModule.DefaultSecondsMatchingPeriod,
            Action<IPBanningModule>? configure = null)
            where TContainer : class, IWebModuleContainer =>
            WithModule(@this, new IPBanningModule("/", failRegex, whitelist, banMinutes,maxRequestPerSecond, maxMatchCount, secondsMatchingPeriod), configure);
    }
}

using EmbedIO.Security;
using System;
using System.Collections.Generic;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Withes the ip banning.
        /// </summary>
        /// <typeparam name="TContainer">The type of the container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="configure">A callback used to configure the module.</param>
        /// <param name="banTime">The time that an IP will remain ban, in minutes.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        /// <returns><paramref name="this"/> with a <see cref="IPBanningModule"/> added.</returns>
        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            Action<IPBanningModule>? configure = null,
            int banTime = IPBanningModule.DefaultBanTime,
            int maxRetry = IPBanningModule.DefaultMaxRetry)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, null, null, banTime, maxRetry, configure);

        /// <summary>
        /// Creates an instance of <see cref="IPBanningModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="failRegex">A collection of regex to match the log messages against.</param>
        /// <param name="banTime">The time that an IP will remain ban, in minutes.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        /// <returns><paramref name="this"/> with a <see cref="IPBanningModule"/> added.</returns>
        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string> failRegex,
            int banTime = IPBanningModule.DefaultBanTime,
            int maxRetry = IPBanningModule.DefaultMaxRetry)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, null, banTime, maxRetry);

        /// <summary>
        /// Creates an instance of <see cref="IPBanningModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="failRegex">A collection of regex to match the log messages against.</param>
        /// <param name="whitelist">A collection of valid IPs that never will be banned.</param>
        /// <param name="banTime">The time that an IP will remain ban, in minutes.</param>
        /// <param name="maxRetry">The maximum number of failed attempts before banning an IP.</param>
        /// <returns><paramref name="this"/> with a <see cref="IPBanningModule"/> added.</returns>
        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string>? failRegex,
            IEnumerable<string>? whitelist = null,
            int banTime = IPBanningModule.DefaultBanTime,
            int maxRetry = IPBanningModule.DefaultMaxRetry)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, null, banTime, maxRetry, null);

        private static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string>? failRegex,
            IEnumerable<string>? whitelist = null,
            int banTime = IPBanningModule.DefaultBanTime,
            int maxRetry = IPBanningModule.DefaultMaxRetry,
            Action<IPBanningModule>? configure = null)
            where TContainer : class, IWebModuleContainer =>
            WithModule(@this, new IPBanningModule("/", failRegex, whitelist, banTime, maxRetry), configure);
    }
}

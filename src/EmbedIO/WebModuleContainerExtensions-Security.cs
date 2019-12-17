using EmbedIO.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        public static TContainer WithIPBanning<TContainer>(this TContainer @this, IEnumerable<string> failRegex)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, null, IPBanningModule.DefaultBanTime, IPBanningModule.DefaultMaxRetry);

        public static TContainer WithIPBanning<TContainer>(this TContainer @this, IEnumerable<string> failRegex, IEnumerable<string>? whitelist)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, whitelist, IPBanningModule.DefaultBanTime, IPBanningModule.DefaultMaxRetry);

        public static TContainer WithIPBanning<TContainer>(this TContainer @this, IEnumerable<string> failRegex, int banTime)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, null, banTime, IPBanningModule.DefaultMaxRetry);

        public static TContainer WithIPBanning<TContainer>(this TContainer @this, 
            IEnumerable<string> failRegex,
            IEnumerable<string>? whitelist,
            int banTime)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, whitelist, banTime, IPBanningModule.DefaultMaxRetry);

        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string> failRegex,
            int banTime,
            int maxRetry)
            where TContainer : class, IWebModuleContainer =>
            WithIPBanning(@this, failRegex, null, banTime, maxRetry);

        public static TContainer WithIPBanning<TContainer>(this TContainer @this,
            IEnumerable<string> failRegex,
            IEnumerable<string>? whitelist,
            int banTime,
            int maxRetry)
            where TContainer : class, IWebModuleContainer =>
            WithModule(@this, new IPBanningModule("/", failRegex, whitelist, banTime, maxRetry));
    }
}

using EmbedIO.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        public static TContainer WithIPBanning<TContainer>(this TContainer @this)
            where TContainer : class, IWebModuleContainer =>
            WithModule(@this, new IPBanningModule("/"));

        public static TContainer WithIPBanning<TContainer>(this TContainer @this, int banTime)
            where TContainer : class, IWebModuleContainer =>
            WithModule(@this, new IPBanningModule("/"));

        public static TContainer WithIPBanning<TContainer>(this TContainer @this, int banTime, int maxRetry)
            where TContainer : class, IWebModuleContainer =>
            WithModule(@this, new IPBanningModule("/"));
    }
}

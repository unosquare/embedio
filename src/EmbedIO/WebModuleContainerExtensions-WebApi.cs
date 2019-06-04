﻿using System;
using EmbedIO.Routing;
using EmbedIO.Utilities;
using EmbedIO.WebApi;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="configure">A callback used to configure the newly-created <see cref="WebApiModule"/>.</param>
        /// <returns><paramref name="this"/> with a <see cref="RoutingModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <seealso cref="WebApiModule"/>
        /// <seealso cref="WebApiModuleExtensions"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithWebApi<TContainer>(this TContainer @this, string baseUrlPath, Action<WebApiModule> configure)
            where TContainer : class, IWebModuleContainer
        {
            configure = Validate.NotNull(nameof(configure), configure);

            var module = new WebApiModule(baseUrlPath);
            configure(module);
            @this.Modules.Add(module);

            return @this;
        }
    }
}
using EmbedIO.Routing;
using EmbedIO.Utilities;
using Swan;
using Swan.Collections;
using System;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="RoutingModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="configure">A callback used to configure the newly-created <see cref="RoutingModule"/>.</param>
        /// <returns><paramref name="this"/> with a <see cref="RoutingModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <seealso cref="RoutingModule"/>
        /// <seealso cref="RoutingModuleExtensions"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithRouting<TContainer>(this TContainer @this, string baseUrlPath, Action<RoutingModule> configure)
            where TContainer : class, IWebModuleContainer
            => WithRouting(@this, null, baseUrlPath, configure);

        /// <summary>
        /// Creates an instance of <see cref="RoutingModule"/> and adds it to a module container,
        /// giving it the specified <paramref name="name"/> if not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="configure">A callback used to configure the newly-created <see cref="RoutingModule"/>.</param>
        /// <returns><paramref name="this"/> with a <see cref="RoutingModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <seealso cref="RoutingModule"/>
        /// <seealso cref="RoutingModuleExtensions"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithRouting<TContainer>(this TContainer @this, string name, string baseUrlPath, Action<RoutingModule> configure)
            where TContainer : class, IWebModuleContainer
        {
            configure = Validate.NotNull(nameof(configure), configure);
            var module = new RoutingModule(baseUrlPath);
            return WithModule(@this, name, module, configure);
        }
    }
}
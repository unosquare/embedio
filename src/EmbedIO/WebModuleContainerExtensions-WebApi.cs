using System;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Utilities;
using EmbedIO.WebApi;
using Swan;
using Swan.Collections;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/> using the default response serializer
        /// and adds it to a module container.
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

        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/> using the specified response serializer
        /// and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="serializer">A <see cref="ResponseSerializerCallback"/> used to serialize
        /// the result of controller methods returning <see langword="object"/>
        /// or <see cref="Task{TResult}">Task&lt;object&gt;</see>.</param>
        /// <param name="configure">A callback used to configure the newly-created <see cref="WebApiModule"/>.</param>
        /// <returns><paramref name="this"/> with a <see cref="RoutingModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="serializer"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="configure"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <seealso cref="WebApiModule"/>
        /// <seealso cref="WebApiModuleExtensions"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithWebApi<TContainer>(
            this TContainer @this,
            string baseUrlPath,
            ResponseSerializerCallback serializer,
            Action<WebApiModule> configure)
            where TContainer : class, IWebModuleContainer
        {
            configure = Validate.NotNull(nameof(configure), configure);

            var module = new WebApiModule(baseUrlPath, serializer);
            configure(module);
            @this.Modules.Add(module);

            return @this;
        }
    }
}
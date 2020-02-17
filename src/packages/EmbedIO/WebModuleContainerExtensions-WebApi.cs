using System;
using System.Threading.Tasks;
using EmbedIO.Routing;
using EmbedIO.Utilities;
using EmbedIO.WebApi;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/> using the default response serializer
        /// and adds it to a module container without giving it a name.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseRoute">The base route of the module.</param>
        /// <param name="configure">A callback used to configure the newly-created <see cref="WebApiModule"/>.</param>
        /// <returns><paramref name="this"/> with a <see cref="RoutingModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <seealso cref="WebApiModule"/>
        /// <seealso cref="WebApiModuleExtensions"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithWebApi<TContainer>(this TContainer @this, string baseRoute, Action<WebApiModule> configure)
            where TContainer : class, IWebModuleContainer
            => WithWebApi(@this, null, baseRoute, configure);

        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/> using the specified response serializer
        /// and adds it to a module container without giving it a name.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseRoute">The base route of the module.</param>
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
            string baseRoute,
            ResponseSerializerCallback serializer,
            Action<WebApiModule> configure)
            where TContainer : class, IWebModuleContainer
            => WithWebApi(@this, null, baseRoute, serializer, configure);

        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/> using the default response serializer
        /// and adds it to a module container, giving it the specified <paramref name="name"/>
        /// if not <see langword="null"/>
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="baseRoute">The base route of the module.</param>
        /// <param name="configure">A callback used to configure the newly-created <see cref="WebApiModule"/>.</param>
        /// <returns><paramref name="this"/> with a <see cref="RoutingModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <seealso cref="WebApiModule"/>
        /// <seealso cref="WebApiModuleExtensions"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithWebApi<TContainer>(
            this TContainer @this,
            string? name,
            string baseRoute,
            Action<WebApiModule> configure)
            where TContainer : class, IWebModuleContainer
        {
            configure = Validate.NotNull(nameof(configure), configure);
            var module = new WebApiModule(baseRoute);
            return WithModule(@this, name, module, configure);
        }

        /// <summary>
        /// Creates an instance of <see cref="WebApiModule"/>, using the specified response serializer
        /// and adds it to a module container, giving it the specified <paramref name="name"/>
        /// if not <see langword="null"/>
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="baseRoute">The base route of the module.</param>
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
            string? name,
            string baseRoute,
            ResponseSerializerCallback serializer,
            Action<WebApiModule> configure)
            where TContainer : class, IWebModuleContainer
        {
            configure = Validate.NotNull(nameof(configure), configure);
            var module = new WebApiModule(baseRoute, serializer);
            return WithModule(@this, name, module, configure);
        }
    }
}
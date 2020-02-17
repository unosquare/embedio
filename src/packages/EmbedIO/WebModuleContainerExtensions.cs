using System;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Contains extension methods for types implementing <see cref="IWebModuleContainer"/>.
    /// </summary>
    public static partial class WebModuleContainerExtensions
    {
        /// <summary>
        /// Adds the specified <paramref name="module"/> to a module container, without giving it a name.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="module">The module.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithModule<TContainer>(this TContainer @this, IWebModule module)
            where TContainer : class, IWebModuleContainer
            => WithModule(@this, null, module);

        /// <summary>
        /// Adds the specified <paramref name="module"/> to a module container,
        /// giving it the specified <paramref name="name"/> if not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="module">The module.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithModule<TContainer>(this TContainer @this, string? name, IWebModule module)
            where TContainer : class, IWebModuleContainer
        {
            @this.Modules.Add(name, module);
            return @this;
        }

        /// <summary>
        /// Adds the specified <paramref name="module"/> to a module container, without giving it a name.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <typeparam name="TWebModule">The type of the <paramref name="module"/>.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="module">The module.</param>
        /// <param name="configure">A callback used to configure the <paramref name="module"/>.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithModule<TContainer, TWebModule>(this TContainer @this, TWebModule module, Action<TWebModule>? configure)
            where TContainer : class, IWebModuleContainer
            where TWebModule : IWebModule
        => WithModule(@this, null, module, configure);

        /// <summary>
        /// Adds the specified <paramref name="module"/> to a module container,
        /// giving it the specified <paramref name="name"/> if not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <typeparam name="TWebModule">The type of the <paramref name="module"/>.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="module">The module.</param>
        /// <param name="configure">A callback used to configure the <paramref name="module"/>.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithModule<TContainer, TWebModule>(this TContainer @this, string? name, TWebModule module, Action<TWebModule>? configure)
            where TContainer : class, IWebModuleContainer
            where TWebModule : IWebModule
        {
            configure?.Invoke(module);
            @this.Modules.Add(name, module);
            return @this;
        }
    }
}
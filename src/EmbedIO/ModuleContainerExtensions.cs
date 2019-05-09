﻿using System;
using EmbedIO.Modules;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Contains extension methods for types implementing <see cref="IWebModuleContainer"/>.
    /// </summary>
    public static class ModuleContainerExtensions
    {
        /// <summary>
        /// Adds the specified <paramref name="module"/> to a module container, without giving it a name.
        /// </summary>
        /// <param name="this">The <see cref="IWebModuleContainer"/> interface on which this method is called.</param>
        /// <param name="module">The module.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static IWebModuleContainer WithModule(this IWebModuleContainer @this, IWebModule module)
            => WithModule(@this, null, module);

        /// <summary>
        /// Adds the specified <paramref name="module"/> to a module container,
        /// giving it the specified <paramref name="name"/> if not <see langword="null"/>.
        /// </summary>
        /// <param name="this">The <see cref="IWebModuleContainer"/> interface on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="module">The module.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static IWebModuleContainer WithModule(this IWebModuleContainer @this, string name, IWebModule module)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            @this.Modules.Add(name, module);
            return @this;
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="TModule"/> and adds it to a module container,
        /// without giving it a name.
        /// </summary>
        /// <typeparam name="TModule">The type of the module to add.</typeparam>
        /// <param name="this">The <see cref="IWebModuleContainer"/> interface on which this method is called.</param>
        /// <param name="module">The module.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static IWebModuleContainer With<TModule>(this IWebModuleContainer @this)
            where TModule : class, IWebModule, new()
            => With<TModule>(@this, null);

        /// <summary>
        /// Creates an instance of <typeparamref name="TModule"/> and adds it to a module container,
        /// giving it the specified <paramref name="name"/> if not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TModule">The type of the module to add.</typeparam>
        /// <param name="this">The <see cref="IWebModuleContainer"/> interface on which this method is called.</param>
        /// <param name="name">The name.</param>
        /// <param name="module">The module.</param>
        /// <returns><paramref name="this"/> with <paramref name="module"/> added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static IWebModuleContainer With<TModule>(this IWebModuleContainer @this, string name)
            where TModule : class, IWebModule, new()
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            @this.Modules.Add(name, new TModule());
            return @this;
        }

        /// <summary>
        /// Creates an instance of <see cref="StaticFilesModule"/> and adds it to a module container.
        /// </summary>
        /// <param name="this">The <see cref="IWebModuleContainer"/> interface on which this method is called.</param>
        /// <param name="fileSystemPath">The path of the directory to serve.</param>
        /// <param name="fileCachingMode">The file caching mode.</param>
        /// <param name="defaultDocument">The default document name.</param>
        /// <param name="defaultExtension">The default document extension.</param>
        /// <param name="useDirectoryBrowser">If set to <see langword="true"/>,
        /// requests mapped to a directory where the default document was not specified or is not found
        /// will return a listing of the directory itself; if set to <see langword="false"/>,
        /// those requests will fail as if the directory was not found.</param>
        /// <returns><paramref name="this"/> with a <see cref="StaticFilesModule"/> added.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="StaticFilesModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static IWebModuleContainer WithStaticFolderAt(
            this IWebModuleContainer @this,
            string fileSystemPath,
            FileCachingMode fileCachingMode = FileCachingMode.Complete,
            string defaultDocument = StaticFilesModule.DefaultDocumentName,
            string defaultExtension = null,
            bool useDirectoryBrowser = false)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

            @this.Modules.Add(new StaticFilesModule(fileSystemPath, fileCachingMode, defaultDocument, defaultExtension, useDirectoryBrowser));
            return @this;
        }
    }
}
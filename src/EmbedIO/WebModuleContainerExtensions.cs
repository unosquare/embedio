using System;
using EmbedIO.Constants;
using EmbedIO.Modules;
using EmbedIO.Utilities;

namespace EmbedIO
{
    /// <summary>
    /// Contains extension methods for types implementing <see cref="IWebModuleContainer"/>.
    /// </summary>
    public static class WebModuleContainerExtensions
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
        public static TContainer WithModule<TContainer>(this TContainer @this, string name, IWebModule module)
            where TContainer : class, IWebModuleContainer
        {
            @this.Modules.Add(name, module);
            return @this;
        }

        /// <summary>
        /// Creates an instance of <see cref="CorsModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="origins">The valid origins, default all.</param>
        /// <param name="headers">The valid headers, default all.</param>
        /// <param name="methods">The valid method, default all.</param>
        /// <returns>
        /// An instance of the tiny web server used to handle request.
        /// </returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="CorsModule"/>
        public static TContainer WithCors<TContainer>(
            this TContainer @this,
            string baseUrlPath,
            string origins = Strings.CorsWildcard,
            string headers = Strings.CorsWildcard,
            string methods = Strings.CorsWildcard)
            where TContainer : class, IWebModuleContainer
        {
            @this.Modules.Add(new CorsModule(baseUrlPath, origins, headers, methods));
            return @this;
        }

        /// <summary>
        /// Creates an instance of <see cref="StaticFilesModule"/> and adds it to a module container.
        /// </summary>
        /// <typeparam name="TContainer">The type of the module container.</typeparam>
        /// <param name="this">The <typeparamref name="TContainer"/> on which this method is called.</param>
        /// <param name="baseUrlPath">The base URL path of the module.</param>
        /// <param name="fileSystemPath">The path of the directory to serve.</param>
        /// <param name="fileCachingMode">The file caching mode.</param>
        /// <param name="defaultDocument">The default document name.</param>
        /// <param name="defaultExtension">The default document extension.</param>
        /// <param name="useDirectoryBrowser">If set to <see langword="true"/>,
        /// requests mapped to a directory where the default document was not specified or is not found
        /// will return a listing of the directory itself; if set to <see langword="false"/>,
        /// those requests will fail as if the directory was not found.</param>
        /// <returns><paramref name="this"/> with a <see cref="StaticFilesModule"/> added.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <seealso cref="StaticFilesModule"/>
        /// <seealso cref="IWebModuleContainer.Modules"/>
        /// <seealso cref="IComponentCollection{T}.Add"/>
        public static TContainer WithStaticFolderAt<TContainer>(
            this TContainer @this,
            string baseUrlPath,
            string fileSystemPath,
            FileCachingMode fileCachingMode = FileCachingMode.Complete,
            string defaultDocument = StaticFilesModule.DefaultDocumentName,
            string defaultExtension = null,
            bool useDirectoryBrowser = false)
            where TContainer : class, IWebModuleContainer
        {
            @this.Modules.Add(new StaticFilesModule(baseUrlPath, fileSystemPath, fileCachingMode, defaultDocument, defaultExtension, useDirectoryBrowser));
            return @this;
        }
    }
}
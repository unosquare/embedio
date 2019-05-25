using System;
using EmbedIO.Files;
using EmbedIO.Utilities;

namespace EmbedIO
{
    partial class WebModuleContainerExtensions
    {
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
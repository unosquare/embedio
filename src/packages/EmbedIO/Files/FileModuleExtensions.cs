using System;

namespace EmbedIO.Files
{
    /// <summary>
    /// Provides extension methods for <see cref="FileModule"/> and derived classes.
    /// </summary>
    public static class FileModuleExtensions
    {
        /// <summary>
        /// Sets the <see cref="FileCache"/> used by a module to store hashes and,
        /// optionally, file contents and rendered directory listings.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">An instance of <see cref="FileCache"/>.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.Cache">Cache</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <seealso cref="FileModule.Cache"/>
        public static TModule WithCache<TModule>(this TModule @this, FileCache value)
            where TModule : FileModule
        {
            @this.Cache = value;
            return @this;
        }

        /// <summary>
        /// Sets a value indicating whether a module caches the contents of files
        /// and directory listings.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value"><see langword="true"/> to enable caching of contents;
        /// <see langword="false"/> to disable it.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.ContentCaching">ContentCaching</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.ContentCaching"/>
        public static TModule WithContentCaching<TModule>(this TModule @this, bool value)
            where TModule : FileModule
        {
            @this.ContentCaching = value;
            return @this;
        }   

        /// <summary>
        /// Enables caching of file contents and directory listings on a module.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.ContentCaching">ContentCaching</see> property
        /// set to <see langword="true"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.ContentCaching"/>
        public static TModule WithContentCaching<TModule>(this TModule @this)
            where TModule : FileModule
        {
            @this.ContentCaching = true;
            return @this;
        }

        /// <summary>
        /// Enables caching of file contents and directory listings on a module.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="maxFileSizeKb"><see langword="true"/> sets the maximum size of a single cached file in kilobytes</param>
        /// <param name="maxSizeKb"><see langword="true"/> sets the maximum total size of cached data in kilobytes</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.ContentCaching">ContentCaching</see> property
        /// set to <see langword="true"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.ContentCaching"/>
        public static TModule WithContentCaching<TModule>(this TModule @this, int maxFileSizeKb, int maxSizeKb)
            where TModule : FileModule
        {
            @this.ContentCaching = true;
            @this.Cache.MaxFileSizeKb = maxFileSizeKb;
            @this.Cache.MaxSizeKb = maxSizeKb;
            return @this;
        }    
        
        /// <summary>
        /// Disables caching of file contents and directory listings on a module.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.ContentCaching">ContentCaching</see> property
        /// set to <see langword="false"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.ContentCaching"/>
        public static TModule WithoutContentCaching<TModule>(this TModule @this)
            where TModule : FileModule
        {
            @this.ContentCaching = false;
            return @this;
        }

        /// <summary>
        /// Sets the name of the default document served, if it exists, instead of a directory listing
        /// when the path of a requested URL maps to a directory.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">The name of the default document.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.DefaultDocument">DefaultDocument</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.DefaultDocument"/>
        public static TModule WithDefaultDocument<TModule>(this TModule @this, string value)
            where TModule : FileModule
        {
            @this.DefaultDocument = value;
            return @this;
        }

        /// <summary>
        /// Sets the name of the default document to <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.DefaultDocument">DefaultDocument</see> property
        /// set to <see langword="null"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.DefaultDocument"/>
        public static TModule WithoutDefaultDocument<TModule>(this TModule @this)
            where TModule : FileModule
        {
            @this.DefaultDocument = null;
            return @this;
        }

        /// <summary>
        /// Sets the default extension appended to requested URL paths that do not map
        /// to any file or directory.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">The default extension.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.DefaultExtension">DefaultExtension</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is a non-<see langword="null"/>,
        /// non-empty string that does not start with a period (<c>.</c>).</exception>
        /// <seealso cref="FileModule.DefaultExtension"/>
        public static TModule WithDefaultExtension<TModule>(this TModule @this, string value)
            where TModule : FileModule
        {
            @this.DefaultExtension = value;
            return @this;
        }

        /// <summary>
        /// Sets the default extension  to <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.DefaultExtension">DefaultExtension</see> property
        /// set to <see langword="null"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.DefaultExtension"/>
        public static TModule WithoutDefaultExtension<TModule>(this TModule @this)
            where TModule : FileModule
        {
            @this.DefaultExtension = null;
            return @this;
        }

        /// <summary>
        /// Sets the <see cref="IDirectoryLister"/> interface used to generate
        /// directory listing in a module.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="value">An <see cref="IDirectoryLister"/> interface, or <see langword="null"/>
        /// to disable the generation of directory listings.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.DirectoryLister">DirectoryLister</see> property
        /// set to <paramref name="value"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.DirectoryLister"/>
        public static TModule WithDirectoryLister<TModule>(this TModule @this, IDirectoryLister value)
            where TModule : FileModule
        {
            @this.DirectoryLister = value;
            return @this;
        }

        /// <summary>
        /// Sets a module's <see cref="FileModule.DirectoryLister">DirectoryLister</see> property
        /// to <see langword="null"/>, disabling the generation of directory listings.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.DirectoryLister">DirectoryLister</see> property
        /// set to <see langword="null"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <seealso cref="FileModule.DirectoryLister"/>
        public static TModule WithoutDirectoryLister<TModule>(this TModule @this)
            where TModule : FileModule
        {
            @this.DirectoryLister = null;
            return @this;
        }

        /// <summary>
        /// Sets a <see cref="FileRequestHandlerCallback"/> that is called by a module whenever
        /// the requested URL path could not be mapped to any file or directory.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="callback">The method to call.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.OnMappingFailed">OnMappingFailed</see> property
        /// set to <paramref name="callback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        /// <seealso cref="FileModule.OnMappingFailed"/>
        /// <seealso cref="FileRequestHandler"/>
        public static TModule HandleMappingFailed<TModule>(this TModule @this, FileRequestHandlerCallback callback)
            where TModule : FileModule
        {
            @this.OnMappingFailed = callback;
            return @this;
        }

        /// <summary>
        /// Sets a <see cref="FileRequestHandlerCallback"/> that is called by a module whenever
        /// the requested URL path has been mapped to a directory, but directory listing has been
        /// disabled.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="callback">The method to call.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.OnDirectoryNotListable">OnDirectoryNotListable</see> property
        /// set to <paramref name="callback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        /// <seealso cref="FileModule.OnDirectoryNotListable"/>
        /// <seealso cref="FileRequestHandler"/>
        public static TModule HandleDirectoryNotListable<TModule>(this TModule @this, FileRequestHandlerCallback callback)
            where TModule : FileModule
        {
            @this.OnDirectoryNotListable = callback;
            return @this;
        }

        /// <summary>
        /// Sets a <see cref="FileRequestHandlerCallback"/> that is called by a module whenever
        /// the requested URL path has been mapped to a file or directory, but the request's
        /// HTTP method is neither <c>GET</c> nor <c>HEAD</c>.
        /// </summary>
        /// <typeparam name="TModule">The type of the module on which this method is called.</typeparam>
        /// <param name="this">The module on which this method is called.</param>
        /// <param name="callback">The method to call.</param>
        /// <returns><paramref name="this"/> with its <see cref="FileModule.OnMethodNotAllowed">OnMethodNotAllowed</see> property
        /// set to <paramref name="callback"/>.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The configuration of <paramref name="this"/> is locked.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
        /// <seealso cref="FileModule.OnMethodNotAllowed"/>
        /// <seealso cref="FileRequestHandler"/>
        public static TModule HandleMethodNotAllowed<TModule>(this TModule @this, FileRequestHandlerCallback callback)
            where TModule : FileModule
        {
            @this.OnMethodNotAllowed = callback;
            return @this;
        }
    }
}

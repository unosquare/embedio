namespace EmbedIO.Modules
{
    /// <summary>
    /// Specifies the file caching modes for <see cref="StaticFilesModule"/>.
    /// </summary>
    public enum FileCachingMode
    {
        /// <summary>
        /// A <see cref="StaticFilesModule"/> uses no caching.
        /// </summary>
        None,

        /// <summary>
        /// A <see cref="StaticFilesModule"/> caches URL paths with corresponding
        /// file system entries, but stores no contents in RAM cache.
        /// </summary>
        MappingOnly,

        /// <summary>
        /// A <see cref="StaticFilesModule"/> caches URL paths with corresponding
        /// file system entries, and stores the contents of requested files in RAM cache.
        /// </summary>
        Complete,
    }
}
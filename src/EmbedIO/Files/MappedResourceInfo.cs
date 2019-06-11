using System;

namespace EmbedIO.Files
{
    /// <summary>
    /// Base class for resources returned by <see cref="IFileProvider"/>.
    /// </summary>
    public abstract class MappedResourceInfo
    {
        private protected MappedResourceInfo(string path, string name, DateTime lastWriteTimeUtc)
        {
            Path = path;
            Name = name;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        /// <summary>
        /// Gets a unique, provider-specific path for the resource.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the name of the resource, as it would appear in a directory listing.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the UTC date and time of the last modification made to the resource.
        /// </summary>
        public DateTime LastWriteTimeUtc { get; }
    }
}
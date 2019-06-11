using System;

namespace EmbedIO.Files
{
    /// <summary>
    /// Contains information about a directory served by a <see cref="IFileProvider"/>.
    /// </summary>
    public class MappedDirectoryInfo : MappedResourceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappedDirectoryInfo"/> class.
        /// </summary>
        /// <param name="path">A unique, provider-specific path for the directory.</param>
        /// <param name="name">The name of the directory, as it would appear in a directory listing.</param>
        /// <param name="lastWriteTimeUtc">The UTC date and time of the last modification made to the directory.</param>
        public MappedDirectoryInfo(string path, string name, DateTime lastWriteTimeUtc)
            : base(path, name, lastWriteTimeUtc)
        {
        }
    }
}
using System;

namespace EmbedIO.Files
{
    /// <summary>
    /// Contains information about a file served by a <see cref="IFileProvider"/>.
    /// </summary>
    public sealed class MappedFileInfo : MappedResourceInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappedFileInfo"/> class.
        /// </summary>
        /// <param name="path">A unique, provider-specific path for the file.</param>
        /// <param name="name">The name of the file, as it would appear in a directory listing.</param>
        /// <param name="lastWriteTimeUtc">The UTC date and time of the last modification made to the file.</param>
        /// <param name="size">The size of the file, expressed in bytes.</param>
        /// <param name="contentType">A MIME type describing the kind of contents of the file.</param>
        public MappedFileInfo(string path, string name, DateTime lastWriteTimeUtc, long size, string contentType)
            : base(path, name, lastWriteTimeUtc)
        {
            Size = size;
            ContentType = contentType;
        }

        /// <summary>
        /// Gets the size of the file, expressed in bytes..
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// Gets a MIME type describing the kind of contents of the file.
        /// </summary>
        public string ContentType { get; }
    }
}
using System;

namespace EmbedIO.Files
{
    /// <summary>
    /// Contains information about a resource served via an <see cref="IFileProvider"/>.
    /// </summary>
    public sealed class MappedResourceInfo
    {
        private MappedResourceInfo(string path, string name, DateTime lastModifiedUtc, long length, string? contentType)
        {
            Path = path;
            Name = name;
            LastModifiedUtc = lastModifiedUtc;
            Length = length;
            ContentType = contentType;
        }

        /// <summary>
        /// Gets a value indicating whether this instance represents a directory.
        /// </summary>
        public bool IsDirectory => ContentType == null;

        /// <summary>
        /// Gets a value indicating whether this instance represents a file.
        /// </summary>
        public bool IsFile => ContentType != null;

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
        public DateTime LastModifiedUtc { get; }

        /// <summary>
        /// <para>If <see cref="IsDirectory"/> is <see langword="false"/>, gets the length of the file, expressed in bytes.</para>
        /// <para>If <see cref="IsDirectory"/> is <see langword="true"/>, this property is always zero.</para>
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// <para>If <see cref="IsDirectory"/> is <see langword="false"/>, gets a MIME type describing the kind of contents of the file.</para>
        /// <para>If <see cref="IsDirectory"/> is <see langword="true"/>, this property is always <see langword="null"/>.</para>
        /// </summary>
        public string? ContentType { get; }

        /// <summary>
        /// Creates and returns a new instance of the <see cref="MappedResourceInfo"/> class,
        /// representing a file.
        /// </summary>
        /// <param name="path">A unique, provider-specific path for the file.</param>
        /// <param name="name">The name of the file, as it would appear in a directory listing.</param>
        /// <param name="lastModifiedUtc">The UTC date and time of the last modification made to the file.</param>
        /// <param name="size">The length of the file, expressed in bytes.</param>
        /// <param name="contentType">A MIME type describing the kind of contents of the file.</param>
        /// <returns>A newly-constructed instance of <see cref="MappedResourceInfo"/>.</returns>
        public static MappedResourceInfo ForFile(string path, string name, DateTime lastModifiedUtc, long size, string contentType)
            => new MappedResourceInfo(path, name, lastModifiedUtc, size, contentType ?? MimeType.Default);

        /// <summary>
        /// Creates and returns a new instance of the <see cref="MappedResourceInfo"/> class,
        /// representing a directory.
        /// </summary>
        /// <param name="path">A unique, provider-specific path for the directory.</param>
        /// <param name="name">The name of the directory, as it would appear in a directory listing.</param>
        /// <param name="lastModifiedUtc">The UTC date and time of the last modification made to the directory.</param>
        /// <returns>A newly-constructed instance of <see cref="MappedResourceInfo"/>.</returns>
        public static MappedResourceInfo ForDirectory(string path, string name, DateTime lastModifiedUtc)
            => new MappedResourceInfo(path, name, lastModifiedUtc, 0, null);
    }
}
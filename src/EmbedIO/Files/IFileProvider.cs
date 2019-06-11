using System.Collections.Generic;
using System.IO;

namespace EmbedIO.Files
{
    /// <summary>
    /// Represents an object that can provide files and/or directories to be served by a <see cref="FileModule"/>.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Gets a value indicating whether the files and directories provided by this instance
        /// will never change.
        /// </summary>
        bool IsImmutable { get; }

        /// <summary>
        /// Gets a value indicating whether streams returned by the <see cref="OpenFile"/> method
        /// will have their <see cref="Stream.CanSeek">CanSeek</see> property set to <see langword="true"/>.
        /// </summary>
        bool CanSeekFiles { get; }

        /// <summary>
        /// <para>Occurs when a file or directory provided by this instance is modified or removed.</para>
        /// </summary>
        event FileSystemEventHandler ResourceChanged;

        /// <summary>
        /// Maps a URL path to a provider-specific path.
        /// </summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="mimeTypeProvider">An <see cref="IMimeTypeProvider"/> interface to use
        /// for determining the MIME type of a file.</param>
        /// <returns>A provider-specific path identifying a file or directory,
        /// or <see langword="null"/> if this instance cannot provide a resource associated
        /// to <paramref name="urlPath"/>.</returns>
        MappedResourceInfo MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider);

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="path">The provider-specific path for the file.</param>
        /// <returns>
        /// <para>A readable <see cref="Stream"/> of the file's contents.</para>
        /// <para>If the <see cref="CanSeekFiles"/> property is <see langword="true"/>,
        /// the returned stream is expected to also be seekable.</para>
        /// </returns>
        Stream OpenFile(string path);

        /// <summary>
        /// Returns an enumeration of the entries of a directory.
        /// </summary>
        /// <param name="path">The provider-specific path for the directory.</param>
        /// <returns>An enumeration of <see cref="MappedResourceInfo"/> objects identifying the entries
        /// in the directory identified by <paramref name="path"/>.</returns>
        IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path);
    }
}
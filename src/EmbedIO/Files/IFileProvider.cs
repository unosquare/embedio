using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EmbedIO.Files
{
    /// <summary>
    /// Represents an object that can provide files and/or directories to be served by a <see cref="FileModule"/>.
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// <para>Occurs when a file or directory provided by this instance is modified or removed.</para>
        /// <para>The event's parameter is the provider-specific path of the resource that changed.</para>
        /// </summary>
        event Action<string> ResourceChanged;

        /// <summary>
        /// Gets a value indicating whether the files and directories provided by this instance
        /// will never change.
        /// </summary>
        bool IsImmutable { get; }

        /// <summary>
        /// Signals a file provider that the web server is starting.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to stop the web server.</param>
        void Start(CancellationToken cancellationToken);

        /// <summary>
        /// Maps a URL path to a provider-specific path.
        /// </summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="mimeTypeProvider">An <see cref="IMimeTypeProvider"/> interface to use
        /// for determining the MIME type of a file.</param>
        /// <returns>A provider-specific path identifying a file or directory,
        /// or <see langword="null"/> if this instance cannot provide a resource associated
        /// to <paramref name="urlPath"/>.</returns>
        MappedResourceInfo? MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider);

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="path">The provider-specific path for the file.</param>
        /// <returns>
        /// <para>A readable <see cref="Stream"/> of the file's contents.</para>
        /// </returns>
        Stream OpenFile(string path);

        /// <summary>
        /// Returns an enumeration of the entries of a directory.
        /// </summary>
        /// <param name="path">The provider-specific path for the directory.</param>
        /// <param name="mimeTypeProvider">An <see cref="IMimeTypeProvider"/> interface to use
        /// for determining the MIME type of files.</param>
        /// <returns>An enumeration of <see cref="MappedResourceInfo"/> objects identifying the entries
        /// in the directory identified by <paramref name="path"/>.</returns>
        IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider);
    }
}
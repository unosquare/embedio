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
#pragma warning disable CA1003 // Use EventHandler<T> - we use Action<> for performance reasons.
        event Action<string>? ResourceChanged;
#pragma warning restore CA1003

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
        /// <param name="path">The URL path.</param>
        /// <param name="mimeTypeProvider">An <see cref="IMimeTypeProvider"/> interface to use
        /// for determining the MIME type of a file.</param>
        /// <returns>A provider-specific path identifying a file or directory,
        /// or <see langword="null"/> if this instance cannot provide a resource associated
        /// to <paramref name="path"/>.</returns>
        MappedResourceInfo? MapUrlPath(string path, IMimeTypeProvider mimeTypeProvider);

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="providerPath">The provider-specific path for the file.</param>
        /// <returns>
        /// <para>A readable <see cref="Stream"/> of the file's contents.</para>
        /// </returns>
        Stream OpenFile(string providerPath);

        /// <summary>
        /// Returns an enumeration of the entries of a directory.
        /// </summary>
        /// <param name="providerPath">The provider-specific path for the directory.</param>
        /// <param name="mimeTypeProvider">An <see cref="IMimeTypeProvider"/> interface to use
        /// for determining the MIME type of files.</param>
        /// <returns>An enumeration of <see cref="MappedResourceInfo"/> objects identifying the entries
        /// in the directory identified by <paramref name="providerPath"/>.</returns>
        IEnumerable<MappedResourceInfo> GetDirectoryEntries(string providerPath, IMimeTypeProvider mimeTypeProvider);
    }
}
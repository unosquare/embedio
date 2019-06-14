using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EmbedIO.Files
{
    /// <summary>
    /// Represents an object that can render a directory listing to a stream.
    /// </summary>
    public interface IDirectoryLister
    {
        /// <summary>
        /// Gets the MIME type of generated directory listings.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Asynchronously generate a directory listing.
        /// </summary>
        /// <param name="info">A <see cref="MappedResourceInfo"/> containing information about
        /// the directory which is to be listed.</param>
        /// <param name="absoluteUrlPath">The absolute URL path that was mapped to <paramref name="info"/>.</param>
        /// <param name="entries">An enumeration of the entries in the directory represented by <paramref name="info"/>.</param>
        /// <param name="stream">A <see cref="Stream"/> to which the directory listing must be written.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        Task ListDirectoryAsync(
            MappedResourceInfo info,
            string absoluteUrlPath,
            IEnumerable<MappedResourceInfo> entries,
            Stream stream,
            CancellationToken cancellationToken);
    }
}
using EmbedIO.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace EmbedIO.Files
{
    /// <summary>
    /// Provides access to files contained in a <c>.zip</c> file to a <see cref="FileModule"/>.
    /// </summary>
    /// <seealso cref="IFileProvider" />
    public class ZipFileProvider : IDisposable, IFileProvider
    {
        private readonly ZipArchive _zipArchive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFileProvider"/> class.
        /// </summary>
        /// <param name="zipFilePath">The zip file path.</param>
        public ZipFileProvider(string zipFilePath)
            : this(new FileStream(Validate.LocalPath(nameof(zipFilePath), zipFilePath, true), FileMode.Open))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZipFileProvider"/> class.
        /// </summary>
        /// <param name="stream">The stream that contains the archive.</param>
        /// <param name="leaveOpen"><see langword="true"/> to leave the stream open after the web server
        /// is disposed; otherwise, <see langword="false"/>.</param>
        public ZipFileProvider(Stream stream, bool leaveOpen = false)
        {
            _zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ZipFileProvider"/> class.
        /// </summary>
        ~ZipFileProvider()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public event Action<string> ResourceChanged
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public bool IsImmutable => true;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
        }

        /// <inheritdoc />
        public MappedResourceInfo? MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            if (urlPath.Length == 1)
                return null;

            urlPath = Uri.UnescapeDataString(urlPath);

            var entry = _zipArchive.GetEntry(urlPath.Substring(1));
            if (entry == null)
                return null;
            
            return MappedResourceInfo.ForFile(
                entry.FullName, 
                entry.Name, 
                entry.LastWriteTime.DateTime, 
                entry.Length, 
                mimeTypeProvider.GetMimeType(Path.GetExtension(entry.Name)));
        }

        /// <inheritdoc />
        public Stream OpenFile(string path)
            => _zipArchive.GetEntry(path)?.Open() ?? throw new FileNotFoundException($"\"{path}\" cannot be found in Zip archive.");

        /// <inheritdoc />
        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider)
            => Enumerable.Empty<MappedResourceInfo>();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            _zipArchive.Dispose();
        }
    }
}
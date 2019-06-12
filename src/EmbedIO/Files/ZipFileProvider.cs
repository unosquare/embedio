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
    /// Provides access to the ZIP file content to a <see cref="FileModule"/>.
    /// </summary>
    /// <seealso cref="IFileProvider" />
    public class ZipFileProvider : IDisposable, IFileProvider
    {
        private readonly Stream _stream;
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
        /// <param name="stream">The stream.</param>
        public ZipFileProvider(Stream stream)
        {
            _stream = stream;
            _zipArchive = new ZipArchive(_stream, ZipArchiveMode.Read);
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
        public bool CanSeekFiles => true;

        /// <inheritdoc />
        public void Dispose()
        {
            _zipArchive?.Dispose();
            _stream?.Dispose();
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
        }

        /// <inheritdoc />
        public MappedResourceInfo MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            if (urlPath.Length == 1)
                return null;

            var entry = _zipArchive.GetEntry(urlPath.Substring(1));

            if (entry == null)
                return null;
            
            mimeTypeProvider.TryGetMimeType(Path.GetExtension(entry.Name), out var mimeType);
            return new MappedFileInfo(entry.FullName, entry.Name, entry.LastWriteTime.DateTime, entry.Length, mimeType);
        }

        /// <inheritdoc />
        public Stream OpenFile(string path)
        {
            var entry = _zipArchive.GetEntry(path);

            return entry?.Open();
        }

        /// <inheritdoc />
        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider)
            => Enumerable.Empty<MappedResourceInfo>();
    }
}

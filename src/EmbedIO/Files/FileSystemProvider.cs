using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using EmbedIO.Utilities;

namespace EmbedIO.Files
{
    /// <summary>
    /// Provides access to the local file system to a <see cref="FileModule"/>.
    /// </summary>
    /// <seealso cref="IFileProvider" />
    public class FileSystemProvider : IDisposable, IFileProvider
    {
        private readonly FileSystemWatcher? _watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemProvider"/> class.
        /// </summary>
        /// <remarks>
        /// OSX doesn't support <see cref="FileSystemWatcher" />, the parameter <paramref name="isImmutable" /> will be always <see langword="true"/>.
        /// </remarks>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="isImmutable"><see langword="true"/> if files and directories in
        /// <paramref name="fileSystemPath"/> are not expected to change during a web server's
        /// lifetime; <see langword="false"/> otherwise.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileSystemPath"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileSystemPath"/> is not a valid local path.</exception>
        /// <seealso cref="Validate.LocalPath"/>
        public FileSystemProvider(string fileSystemPath, bool isImmutable)
        {
            FileSystemPath = Validate.LocalPath(nameof(fileSystemPath), fileSystemPath, true);
            IsImmutable = isImmutable || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            try
            {
                if (!IsImmutable)
                    _watcher = new FileSystemWatcher(FileSystemPath);
            }
            catch (PlatformNotSupportedException)
            {
                IsImmutable = true;
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FileSystemProvider"/> class.
        /// </summary>
        ~FileSystemProvider()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public event Action<string>? ResourceChanged;

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        public string FileSystemPath { get; }

        /// <inheritdoc />
        public bool IsImmutable { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            if (_watcher != null)
            {
                _watcher.Changed += Watcher_ChangedOrDeleted;
                _watcher.Deleted += Watcher_ChangedOrDeleted;
                _watcher.Renamed += Watcher_Renamed;
                _watcher.EnableRaisingEvents = true;
            }
        }

        /// <inheritdoc />
        public MappedResourceInfo? MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            urlPath = urlPath.Substring(1); // Drop the initial slash
            string localPath;

            // Disable CA1031 as there's little we can do if IsPathRooted or GetFullPath fails.
#pragma warning disable CA1031
            try
            {
                // Unescape the url before continue
                urlPath = Uri.UnescapeDataString(urlPath);

                // Bail out early if the path is a rooted path,
                // as Path.Combine would ignore our base path.
                // See https://docs.microsoft.com/en-us/dotnet/api/system.io.path.combine
                // (particularly the Remarks section).
                //
                // Under Windows, a relative URL path may be a full filesystem path
                // (e.g. "D:\foo\bar" or "\\192.168.0.1\Shared\MyDocuments\BankAccounts.docx").
                // Under Unix-like operating systems we have no such problems, as relativeUrlPath
                // can never start with a slash; however, loading one more class from Swan
                // just to check the OS type would probably outweigh calling IsPathRooted.
                if (Path.IsPathRooted(urlPath))
                    return null;

                // Convert the relative URL path to a relative filesystem path
                // (practically a no-op under Unix-like operating systems)
                // and combine it with our base local path to obtain a full path.
                localPath = Path.Combine(FileSystemPath, urlPath.Replace('/', Path.DirectorySeparatorChar));

                // Use GetFullPath as an additional safety check
                // for relative paths that contain a rooted path
                // (e.g. "valid/path/C:\Windows\System.ini")
                localPath = Path.GetFullPath(localPath);
            }
            catch
            {
                // Both IsPathRooted and GetFullPath throw exceptions
                // if a path contains invalid characters or is otherwise invalid;
                // bail out in this case too, as the path would not exist on disk anyway.
                return null;
            }
#pragma warning restore CA1031

            // As a final precaution, check that the resulting local path
            // is inside the folder intended to be served.
            if (!localPath.StartsWith(FileSystemPath, StringComparison.Ordinal))
                return null;

            if (File.Exists(localPath))
                return GetMappedFileInfo(mimeTypeProvider, localPath);

            if (Directory.Exists(localPath))
                return GetMappedDirectoryInfo(localPath);

            return null;
        }

        /// <inheritdoc />
        public Stream OpenFile(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        /// <inheritdoc />
        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider)
            => new DirectoryInfo(path).EnumerateFileSystemInfos()
                .Select(fsi => GetMappedResourceInfo(mimeTypeProvider, fsi));

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            ResourceChanged = null; // Release references to listeners

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= Watcher_ChangedOrDeleted;
                _watcher.Deleted -= Watcher_ChangedOrDeleted;
                _watcher.Renamed -= Watcher_Renamed;

                if (disposing)
                    _watcher.Dispose();
            }
        }

        private static MappedResourceInfo GetMappedFileInfo(IMimeTypeProvider mimeTypeProvider, string localPath)
            => GetMappedFileInfo(mimeTypeProvider, new FileInfo(localPath));

        private static MappedResourceInfo GetMappedFileInfo(IMimeTypeProvider mimeTypeProvider, FileInfo info)
            => MappedResourceInfo.ForFile(
                info.FullName,
                info.Name,
                info.LastWriteTimeUtc,
                info.Length,
                mimeTypeProvider.GetMimeType(info.Extension));

        private static MappedResourceInfo GetMappedDirectoryInfo(string localPath)
            => GetMappedDirectoryInfo(new DirectoryInfo(localPath));

        private static MappedResourceInfo GetMappedDirectoryInfo(DirectoryInfo info)
            => MappedResourceInfo.ForDirectory(info.FullName, info.Name, info.LastWriteTimeUtc);

        private static MappedResourceInfo GetMappedResourceInfo(IMimeTypeProvider mimeTypeProvider, FileSystemInfo info)
            => info is DirectoryInfo directoryInfo
                ? GetMappedDirectoryInfo(directoryInfo)
                : GetMappedFileInfo(mimeTypeProvider, (FileInfo) info);

        private void Watcher_ChangedOrDeleted(object sender, FileSystemEventArgs e)
            => ResourceChanged?.Invoke(e.FullPath);

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
            => ResourceChanged?.Invoke(e.OldFullPath);
    }
}

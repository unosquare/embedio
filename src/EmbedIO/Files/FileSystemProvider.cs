using EmbedIO.Files.Internal;
using EmbedIO.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace EmbedIO.Files
{
    public class FileSystemProvider : IFileProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemProvider"/> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        public FileSystemProvider(string fileSystemPath)
        {
            FileSystemPath = Validate.LocalPath(nameof(fileSystemPath), fileSystemPath, true);
        }

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        public string FileSystemPath { get; }

        /// <inheritdoc />
        public bool IsImmutable => true;

        /// <inheritdoc />
        public bool CanSeekFiles => true;

        /// <inheritdoc />
        public event FileSystemEventHandler ResourceChanged;

        /// <inheritdoc />
        public MappedResourceInfo MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            var localPath = MapRelativeUrlPathToLoLocalPath(urlPath.Substring(1));

            // Error 404 on failed mapping.
            var pathResult = localPath == null
                ? PathMappingResult.NotFound
                : ValidateLocalPath(ref localPath);

            switch (pathResult)
            {
                case PathMappingResult.IsFile:
                    return GetMappedFileInfo(mimeTypeProvider, localPath);
                case PathMappingResult.IsDirectory:
                    return GetMappedDirectoryInfo(localPath);
                default:
                    return null;
            }
        }

        /// <inheritdoc />
        public Stream OpenFile(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        /// <inheritdoc />
        public IEnumerable<MappedResourceInfo> GetDirectoryEntries(string path, IMimeTypeProvider mimeTypeProvider)
        {
            var entries = Directory.GetFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly);

            foreach (var entry in entries)
            {
                if (File.Exists(entry))
                    yield return GetMappedFileInfo(mimeTypeProvider, path);

                if (Directory.Exists(entry))
                    yield return GetMappedDirectoryInfo(path);
            }
        }

        private string MapRelativeUrlPathToLoLocalPath(string relativeUrlPath)
        {
            string localPath;

            // Disable CA1031 as there's little we can do if IsPathRooted or GetFullPath fails.
#pragma warning disable CA1031
            try
            {
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
                if (Path.IsPathRooted(relativeUrlPath))
                    return null;

                // Convert the relative URL path to a relative filesystem path
                // (practically a no-op under Unix-like operating systems)
                // and combine it with our base local path to obtain a full path.
                localPath = Path.Combine(FileSystemPath, relativeUrlPath.Replace('/', Path.DirectorySeparatorChar));

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

            return localPath;
        }
        
        private static MappedResourceInfo GetMappedFileInfo(IMimeTypeProvider mimeTypeProvider, string localPath)
        {
            var fileInfo = new FileInfo(localPath);
            mimeTypeProvider.TryGetMimeType(fileInfo.Extension, out var mimeType);

            return new MappedFileInfo(localPath, fileInfo.Name, fileInfo.LastWriteTimeUtc, fileInfo.Length, mimeType);
        }
        
        private static MappedResourceInfo GetMappedDirectoryInfo(string localPath)
        {
            var directoryInfo = new DirectoryInfo(localPath);

            return new MappedDirectoryInfo(localPath, directoryInfo.Name, directoryInfo.LastWriteTimeUtc);
        }

        private static PathMappingResult ValidateLocalPath(ref string localPath)
        {
            if (File.Exists(localPath))
                return PathMappingResult.IsFile;

            if (Directory.Exists(localPath))
                return PathMappingResult.IsDirectory;

            return PathMappingResult.NotFound;
        }
    }
}

namespace Unosquare.Labs.EmbedIO.Modules
{
    using System.Collections.Generic;
    using System.IO;
    using System;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Collections.Concurrent;

    internal class VirtualPaths : Dictionary<string, string>
    {
        internal enum VirtualPathStatus
        {
            /// <summary>
            /// The invalid
            /// </summary>
            Invalid,

            /// <summary>
            /// The forbidden
            /// </summary>
            Forbidden,

            /// <summary>
            /// The file
            /// </summary>
            File,

            /// <summary>
            /// The directory
            /// </summary>
            Directory
        }

        private readonly ConcurrentDictionary<string, string> _validPaths = new ConcurrentDictionary<string, string>();

        private readonly ConcurrentDictionary<string, string> _mappedPaths = new ConcurrentDictionary<string, string>();

        public VirtualPaths(string fileSystemPath, bool useDirectoryBrowser)
        {
            FileSystemPath = fileSystemPath;
            UseDirectoryBrowser = useDirectoryBrowser;
        }

        public ReadOnlyDictionary<string, string> Collection => new ReadOnlyDictionary<string, string>(this);

        public string DefaultDocument { get; set; }

        public string DefaultExtension { get; set; }

        public string FileSystemPath { get; }

        public bool UseDirectoryBrowser { get; }

        internal VirtualPathStatus ExistsLocalPath(string urlPath, ref string localPath)
        {
            if (_validPaths.TryGetValue(localPath, out var tempPath))
            {
                localPath = tempPath;
                return UseDirectoryBrowser && Directory.Exists(localPath)
                    ? VirtualPathStatus.Directory
                    : VirtualPathStatus.File;
            }

            // Check if the requested local path is part of the root File System Path
            if (IsPartOfPath(localPath, FileSystemPath) == false)
            {
                return VirtualPathStatus.Forbidden;
            }

            var originalPath = localPath;
            var result = ExistsPath(urlPath, ref localPath);

            if (result != VirtualPathStatus.Invalid)
                _validPaths.TryAdd(originalPath, localPath);

            return result;
        }

        internal void RegisterVirtualPath(string virtualPath, string physicalPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath) || virtualPath == "/" || virtualPath[0] != '/')
                throw new InvalidOperationException($"The virtual path {virtualPath} is invalid");

            if (ContainsKey(virtualPath))
                throw new InvalidOperationException($"The virtual path {virtualPath} already exists");

            if (!Directory.Exists(physicalPath))
                throw new InvalidOperationException($"The physical path {physicalPath} doesn't exist");

            physicalPath = Path.GetFullPath(physicalPath);
            Add(virtualPath, physicalPath);
        }

        internal void UnregisterVirtualPath(string virtualPath)
        {
            if (ContainsKey(virtualPath) == false)
                throw new InvalidOperationException($"The virtual path {virtualPath} doesn't exists");

            Remove(virtualPath);
        }

        internal string GetUrlPath(string requestPath, ref string baseLocalPath)
        {
            if (_mappedPaths.TryGetValue(requestPath, out var urlPath))
            {
                return urlPath;
            }

            urlPath = requestPath.Replace('/', Path.DirectorySeparatorChar);

            if (this.Any(x => requestPath.StartsWith(x.Key)))
            {
                var additionalPath = this.FirstOrDefault(x => requestPath.StartsWith(x.Key));
                baseLocalPath = additionalPath.Value;
                urlPath = urlPath.Replace(additionalPath.Key.Replace('/', Path.DirectorySeparatorChar), string.Empty);

                if (string.IsNullOrWhiteSpace(urlPath))
                {
                    urlPath = Path.DirectorySeparatorChar.ToString();
                }
            }

            // adjust the path to see if we've got a default document
            if (!UseDirectoryBrowser && urlPath.Last() == Path.DirectorySeparatorChar)
            {
                urlPath = urlPath + DefaultDocument;
            }

            urlPath = urlPath.TrimStart(Path.DirectorySeparatorChar);

            _mappedPaths.TryAdd(requestPath, urlPath);

            return urlPath;
        }

        private static bool IsPartOfPath(string targetPath, string basePath)
        {
            targetPath = Path.GetFullPath(targetPath).ToLowerInvariant().TrimEnd('/', '\\');
            basePath = Path.GetFullPath(basePath).ToLowerInvariant().TrimEnd('/', '\\');

            return targetPath.StartsWith(basePath);
        }

        private VirtualPathStatus ExistsPath(string urlPath, ref string localPath)
        {
            // check if the path is just a directoy and return
            if (UseDirectoryBrowser && Directory.Exists(localPath))
            {
                return VirtualPathStatus.Directory;
            }

            if (string.IsNullOrWhiteSpace(DefaultExtension) == false && DefaultExtension.StartsWith(".") &&
                File.Exists(localPath) == false)
            {
                localPath += DefaultExtension;
            }

            if (File.Exists(localPath))
            {
                return VirtualPathStatus.File;
            }

            if (File.Exists(Path.Combine(localPath, DefaultDocument)))
            {
                localPath = Path.Combine(localPath, DefaultDocument);
            }
            else
            {
                // Try to fallback to root
                var rootLocalPath = Path.Combine(FileSystemPath, urlPath);

                if (UseDirectoryBrowser && Directory.Exists(rootLocalPath))
                {
                    localPath = rootLocalPath;
                    return VirtualPathStatus.Directory;
                }

                if (File.Exists(rootLocalPath))
                {
                    localPath = rootLocalPath;
                }
                else if (File.Exists(Path.Combine(rootLocalPath, DefaultDocument)))
                {
                    localPath = Path.Combine(rootLocalPath, DefaultDocument);
                }
                else
                {
                    return VirtualPathStatus.Invalid;
                }
            }
            
            return VirtualPathStatus.File;
        }
    }
}
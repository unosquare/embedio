namespace Unosquare.Labs.EmbedIO.Core
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;

    internal sealed class VirtualPathManager : IDisposable
    {
        public const string DefaultDocumentName = "index.html";

        private const string RootUrlPath = "/";

        private readonly SortedDictionary<string, VirtualPath> _virtualPaths = new SortedDictionary<string, VirtualPath>(ReverseOrdinalStringComparer.Instance);

        private readonly VirtualPath _rootPath;

        private readonly ReaderWriterLockSlim _access = new ReaderWriterLockSlim();

        private readonly ConcurrentDictionary<string, PathCacheItem> _pathCache = new ConcurrentDictionary<string, PathCacheItem>();

        private string _defaultExtension;

        private string _defaultDocument = DefaultDocumentName;

        public VirtualPathManager(string rootLocalPath, bool canMapDirectories, bool cachePaths)
        {
            rootLocalPath = PathHelper.EnsureValidLocalPath(rootLocalPath);
            _rootPath = new VirtualPath(RootUrlPath, rootLocalPath);
            CanMapDirectories = canMapDirectories;
            CachePaths = cachePaths;
        }

        ~VirtualPathManager()
        {
            Dispose(false);
        }

        public string RootLocalPath => _rootPath.BaseLocalPath;

        public bool CanMapDirectories { get; }

        public bool CachePaths { get; }

        public string DefaultExtension
        {
            get
            {
                _access.EnterReadLock();
                try
                {
                    return _defaultExtension;
                }
                finally
                {
                    _access.ExitReadLock();
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    value = null;
                }
                else if (value[0] != '.')
                {
                    throw new InvalidOperationException("The default extension, if any, must start with a dot.");
                }

                if (string.Equals(value, _defaultExtension, StringComparison.Ordinal))
                    return;

                _access.EnterWriteLock();
                try
                {
                    _defaultExtension = value;

                    // Discard cache entries for which the previous default extension was used.
                    // If / when requested again, the new default extension will be used.
                    var keys = _pathCache
                        .Where(p => (p.Value.MappingResult & PathMappingResult.DefaultExtensionUsed) != 0)
                        .Select(p => p.Key)
                        .ToArray();
                    foreach (var key in keys)
                    {
                        _pathCache.TryRemove(key, out _);
                    }
                }
                finally
                {
                    _access.ExitWriteLock();
                }
            }
        }

        public string DefaultDocument
        {
            get
            {
                _access.EnterReadLock();
                try
                {
                    return _defaultDocument;
                }
                finally
                {
                    _access.ExitReadLock();
                }
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    value = null;
                }

                if (string.Equals(value, _defaultDocument, StringComparison.Ordinal))
                    return;

                _access.EnterWriteLock();
                try
                {
                    _defaultDocument = value;

                    // Discard cache entries for which the previous default document was used.
                    // If / when requested again, the new default document will be used.
                    var keys = _pathCache
                        .Where(p => (p.Value.MappingResult & PathMappingResult.DefaultDocumentUsed) != 0)
                        .Select(p => p.Key)
                        .ToArray();
                    foreach (var key in keys)
                    {
                        _pathCache.TryRemove(key, out _);
                    }
                }
                finally
                {
                    _access.ExitWriteLock();
                }
            }
        }

        public ReadOnlyDictionary<string, string> VirtualPaths
        {
            get
            {
                IDictionary<string, string> dictionary;

                _access.EnterReadLock();
                try
                {
                    dictionary = _virtualPaths.Values.ToDictionary(p => p.BaseUrlPath, p => p.BaseLocalPath);
                }
                finally
                {
                    _access.ExitReadLock();
                }

                return new ReadOnlyDictionary<string, string>(dictionary);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void RegisterVirtualPath(string virtualPath, string physicalPath)
        {
            virtualPath = PathHelper.EnsureValidUrlPath(virtualPath, true);

            if (virtualPath == RootUrlPath)
                throw new InvalidOperationException($"The virtual path {RootUrlPath} is invalid.");

            physicalPath = PathHelper.EnsureValidLocalPath(physicalPath);

            _access.EnterWriteLock();
            try
            {
                if (_virtualPaths.ContainsKey(virtualPath))
                    throw new InvalidOperationException($"The virtual path {virtualPath} already exists.");

                var vp = new VirtualPath(virtualPath, physicalPath);
                _virtualPaths.Add(virtualPath, vp);

                // Remove URL paths that could be mapped by the new virtual path,
                // but were mapped by either a shorter virtual path, or the root path,
                // from the mapped paths cache.
                // If / when requested again, those paths can now be mapped by the newly-added virtual path.
                var keys = _pathCache
                    .Where(p => vp.CanMapUrlPath(p.Key) && p.Value.BaseUrlPath.Length < virtualPath.Length)
                    .Select(p => p.Key)
                    .ToArray();
                foreach (var key in keys)
                {
                    _pathCache.TryRemove(key, out _);
                }
            }
            finally
            {
                _access.ExitWriteLock();
            }
        }

        public void UnregisterVirtualPath(string virtualPath)
        {
            virtualPath = PathHelper.EnsureValidUrlPath(virtualPath, true);

            _access.EnterWriteLock();
            try
            {
                if (!_virtualPaths.ContainsKey(virtualPath))
                    throw new InvalidOperationException($"The virtual path {virtualPath} does not exist.");

                _virtualPaths.Remove(virtualPath);

                // Remove paths mapped by this virtual path
                // from the mapped paths cache.
                // If / when requested again, those paths will be mapped
                // by either a shorter virtual path, or the root path.
                var keys = _pathCache
                    .Where(p => string.Equals(virtualPath, p.Value.BaseUrlPath, StringComparison.Ordinal))
                    .Select(p => p.Key)
                    .ToArray();
                foreach (var key in keys)
                {
                    _pathCache.TryRemove(key, out _);
                }
            }
            finally
            {
                _access.ExitWriteLock();
            }
        }

        public PathMappingResult MapUrlPath(string urlPath, out string localPath)
        {
            urlPath = PathHelper.NormalizeUrlPath(urlPath, false);
            var result = CachePaths ? _pathCache.GetOrAdd(urlPath, MapUrlPathCore) : MapUrlPathCore(urlPath);
            localPath = result.LocalPath;
            return result.MappingResult;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _access.Dispose();
            }

            _pathCache.Clear();
        }

        private PathCacheItem MapUrlPathCore(string urlPath)
        {
            _access.EnterReadLock();
            try
            {
                var localPath = MapUrlPathToLocalPath(urlPath, out var baseUrlPath);
                var validationResult = ValidateLocalPath(ref localPath);
                return new PathCacheItem(baseUrlPath, localPath, validationResult);
            }
            finally
            {
                _access.ExitReadLock();
            }
        }

        private string MapUrlPathToLocalPath(string urlPath, out string baseUrlPath)
        {
            // Assuming that urlPath is not null, not empty, and starts with a slash,
            // a length lower than 2 can only mean that the path is "/".
            // Bail out early, because we need at least a length of 2
            // for the optimizations below to work.
            if (urlPath.Length < 2)
            {
                baseUrlPath = RootUrlPath;
                return _rootPath.BaseLocalPath;
            }

            string localPath;

            // First try to use each virtual path in reverse ordinal order
            // (so e.g. "/media/images" is evaluated before "/media".)
            // As long as we keep checks simple, we can try to optimize the loop a little.
            // The second character of a URL path is the first character following the initial slash;
            // by checking just that, we can avoid some useless calls to TryMapUrlPathLoLocalPath.
            var secondChar = urlPath[1];
            foreach (var virtualPath in _virtualPaths.Values)
            {
                var baseSecondChar = virtualPath.BaseUrlPath[1];
                if (baseSecondChar == secondChar)
                {
                    // If the second character is the same, try mapping.
                    if (virtualPath.TryMapUrlPathLoLocalPath(urlPath, out localPath))
                    {
                        baseUrlPath = virtualPath.BaseUrlPath;
                        return localPath;
                    }
                }
                else if (baseSecondChar < secondChar)
                {
                    // If we have reached a base URL path with a second character
                    // with a lower value than ours, we can safely bail out of the loop.
                    break;
                }
            }

            // If no virtual path can map our URL path, use the root path.
            // This will always succeed.
            _rootPath.TryMapUrlPathLoLocalPath(urlPath, out localPath);
            baseUrlPath = RootUrlPath;
            return localPath;
        }

        private PathMappingResult ValidateLocalPath(ref string localPath)
        {
            if (File.Exists(localPath))
                return PathMappingResult.IsFile;

            if (Directory.Exists(localPath))
            {
                if (CanMapDirectories)
                    return PathMappingResult.IsDirectory;

                if (_defaultDocument != null)
                {
                    localPath = Path.Combine(localPath, _defaultDocument);
                    if (File.Exists(localPath))
                        return PathMappingResult.IsFile | PathMappingResult.DefaultDocumentUsed;
                }
            }

            if (_defaultExtension != null)
            {
                localPath += _defaultExtension;
                if (File.Exists(localPath))
                    return PathMappingResult.IsFile | PathMappingResult.DefaultExtensionUsed;
            }

            localPath = null;
            return PathMappingResult.NotFound;
        }

        private struct PathCacheItem
        {
            public readonly string BaseUrlPath;

            public readonly string LocalPath;

            public readonly PathMappingResult MappingResult;

            public PathCacheItem(string baseUrlPath, string localPath, PathMappingResult mappingResult)
            {
                BaseUrlPath = baseUrlPath;
                LocalPath = localPath;
                MappingResult = mappingResult;
            }
        }
    }
}
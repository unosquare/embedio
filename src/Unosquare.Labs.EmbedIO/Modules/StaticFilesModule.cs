namespace Unosquare.Labs.EmbedIO.Modules
{
    using Core;
    using Constants;
    using EmbedIO;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Swan;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a simple module to server static files from the file system.
    /// </summary>
    public class StaticFilesModule : FileModuleBase, IDisposable
    {
        /// <summary>
        /// Default document constant to "index.html".
        /// </summary>
        public const string DefaultDocumentName = VirtualPathManager.DefaultDocumentName;

        /// <summary>
        /// Maximal length of entry in DirectoryBrowser.
        /// </summary>
        private const int MaxEntryLength = 50;

        /// <summary>
        /// How many characters used after time in DirectoryBrowser.
        /// </summary>
        private const int SizeIndent = 20;

        private readonly VirtualPathManager _virtualPathManager;

        private readonly ConcurrentDictionary<string, Tuple<long, string>> _fileHashCache =
            new ConcurrentDictionary<string, Tuple<long, string>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public StaticFilesModule(Dictionary<string, string> paths)
            : this(paths.First().Value, null, paths, false, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="useDirectoryBrowser">if set to <c>true</c> [use directory browser].</param>
        public StaticFilesModule(string fileSystemPath, bool useDirectoryBrowser)
            : this(fileSystemPath, null, null, useDirectoryBrowser, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="useDirectoryBrowser">if set to <c>true</c> [use directory browser].</param>
        /// <param name="cacheMappedPaths">if set to <c>true</c>, [cache mapped paths].</param>
        public StaticFilesModule(string fileSystemPath, bool useDirectoryBrowser, bool cacheMappedPaths)
            : this(fileSystemPath, null, null, useDirectoryBrowser, cacheMappedPaths)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="headers">The headers to set in every request.</param>
        /// <param name="additionalPaths">The additional paths.</param>
        /// <param name="useDirectoryBrowser">if set to <c>true</c> [use directory browser].</param>
        /// <exception cref="ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesModule(
            string fileSystemPath,
            Dictionary<string, string> headers,
            Dictionary<string, string> additionalPaths,
            bool useDirectoryBrowser)
            : this(fileSystemPath, headers, additionalPaths, useDirectoryBrowser, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="headers">The headers to set in every request.</param>
        /// <param name="additionalPaths">The additional paths.</param>
        /// <param name="useDirectoryBrowser">if set to <c>true</c> [use directory browser].</param>
        /// <param name="cacheMappedPaths">if set to <c>true</c>, [cache mapped paths].</param>
        /// <exception cref="ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesModule(
            string fileSystemPath,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> additionalPaths = null,
            bool useDirectoryBrowser = false,
            bool cacheMappedPaths = true)
        {
            if (!Directory.Exists(fileSystemPath))
                throw new ArgumentException($"Path '{fileSystemPath}' does not exist.");

            _virtualPathManager = new VirtualPathManager(Path.GetFullPath(fileSystemPath), useDirectoryBrowser, cacheMappedPaths);

            DefaultDocument = DefaultDocumentName;
            UseGzip = true;
#if DEBUG
            // When debugging, disable RamCache
            UseRamCache = false;
#else
            UseRamCache = true;
#endif

            headers?.ForEach(DefaultHeaders.Add);
            additionalPaths?.ForEach((virtualPath, physicalPath) => {
                if (virtualPath != "/")
                    RegisterVirtualPath(virtualPath, physicalPath);
            });

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="StaticFilesModule"/> class.
        /// </summary>
        ~StaticFilesModule()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets the maximum size of the ram cache file. The default value is 250kb.
        /// </summary>
        /// <value>
        /// The maximum size of the ram cache file.
        /// </value>
        public int MaxRamCacheFileSize { get; set; } = 250 * 1024;

        /// <summary>
        /// Gets or sets the default document.
        /// Defaults to "index.html"
        /// Example: "root.xml".
        /// </summary>
        /// <value>
        /// The default document.
        /// </value>
        public string DefaultDocument
        {
            get => _virtualPathManager.DefaultDocument;
            set => _virtualPathManager.DefaultDocument = value;
        }

        /// <summary>
        /// Gets or sets the default extension.
        /// Defaults to null
        /// Example: ".html".
        /// </summary>
        /// <value>
        /// The default extension.
        /// </value>
        public string DefaultExtension
        {
            get => _virtualPathManager.DefaultExtension;
            set => _virtualPathManager.DefaultExtension = value;
        }

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        /// <value>
        /// The file system path.
        /// </value>
        public string FileSystemPath => _virtualPathManager.RootLocalPath;

        /// <summary>
        /// Gets or sets a value indicating whether or not to use the RAM Cache feature
        /// RAM Cache will only cache files that are MaxRamCacheSize in bytes or less.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use ram cache]; otherwise, <c>false</c>.
        /// </value>
        public bool UseRamCache { get; set; }

        /// <summary>
        /// Gets the virtual paths.
        /// </summary>
        /// <value>
        /// The virtual paths.
        /// </value>
        public ReadOnlyDictionary<string, string> VirtualPaths => _virtualPathManager.VirtualPaths;

        /// <inheritdoc />
        public override string Name => nameof(StaticFilesModule);

        /// <summary>
        /// Private collection holding the contents of the RAM Cache.
        /// </summary>
        /// <value>
        /// The ram cache.
        /// </value>
        private RamCache RamCache { get; } = new RamCache();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Registers the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when a method call is invalid for the object's current state.
        /// </exception>
        public void RegisterVirtualPath(string virtualPath, string physicalPath)
            => _virtualPathManager.RegisterVirtualPath(virtualPath, physicalPath);

        /// <summary>
        /// Unregisters the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when a method call is invalid for the object's current state.
        /// </exception>
        public void UnregisterVirtualPath(string virtualPath) => _virtualPathManager.UnregisterVirtualPath(virtualPath);

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache() => RamCache.Clear();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _virtualPathManager.Dispose();
        }

        private static Task<bool> HandleDirectory(IHttpContext context, string localPath, CancellationToken ct)
        {
            var entries = new[] { context.Request.RawUrl == "/" ? string.Empty : "<a href='../'>../</a>" }
                .Concat(
                    Directory.GetDirectories(localPath)
                        .Select(path =>
                        {
                            var name = path.Replace(
                                localPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                                string.Empty);
                            return new
                            {
                                Name = (name + Path.DirectorySeparatorChar).Truncate(MaxEntryLength, "..>"),
                                Url = Uri.EscapeDataString(name) + Path.DirectorySeparatorChar,
                                ModificationTime = new DirectoryInfo(path).LastWriteTimeUtc,
                                Size = "-",
                            };
                        })
                        .OrderBy(x => x.Name)
                        .Union(Directory.GetFiles(localPath, "*", SearchOption.TopDirectoryOnly)
                            .Select(path =>
                            {
                                var fileInfo = new FileInfo(path);
                                var name = Path.GetFileName(path);

                                return new
                                {
                                    Name = name.Truncate(MaxEntryLength, "..>"),
                                    Url = Uri.EscapeDataString(name),
                                    ModificationTime = fileInfo.LastWriteTimeUtc,
                                    Size = fileInfo.Length.FormatBytes(),
                                };
                            })
                            .OrderBy(x => x.Name))
                        .Select(y => $"<a href='{y.Url}'>{System.Net.WebUtility.HtmlEncode(y.Name)}</a>" +
                                     new string(' ', MaxEntryLength - y.Name.Length + 1) +
                                     y.ModificationTime.ToString(Strings.BrowserTimeFormat,
                                         CultureInfo.InvariantCulture) +
                                     new string(' ', SizeIndent - y.Size.Length) +
                                     y.Size))
                .Where(x => !string.IsNullOrWhiteSpace(x));

            var content = Responses.ResponseBaseHtml.Replace(
                "{0}",
                $"<h1>Index of {System.Net.WebUtility.HtmlEncode(context.RequestPathCaseSensitive())}</h1><hr/><pre>{string.Join("\n", entries)}</pre><hr/>");

            return context.HtmlResponseAsync(content, cancellationToken: ct);
        }

        private Task<bool> HandleGet(IHttpContext context, CancellationToken ct, bool sendBuffer = true)
        {
            switch (_virtualPathManager.MapUrlPath(context.RequestPathCaseSensitive(), out var localPath) & PathMappingResult.MappingMask)
            {
                case PathMappingResult.IsFile:
                    return HandleFile(context, localPath, sendBuffer, ct);
                case PathMappingResult.IsDirectory:
                    return HandleDirectory(context, localPath, ct);
                default:
                    return Task.FromResult(false);
            }
        }

        private async Task<bool> HandleFile(
            IHttpContext context,
            string localPath,
            bool sendBuffer,
            CancellationToken ct)
        {
            Stream buffer = null;

            try
            {
                var isTagValid = false;
                var partialHeader = context.RequestHeader(HttpHeaders.Range);
                var usingPartial = partialHeader?.StartsWith("bytes=") == true;
                var fileInfo = new FileInfo(localPath);

                if (sendBuffer)
                    buffer = GetFileStream(context, fileInfo, usingPartial, out isTagValid);

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = fileInfo.LastWriteTimeUtc
                    .ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo);

                if (!usingPartial &&
                    (isTagValid || context.RequestHeader(HttpHeaders.IfModifiedSince).Equals(utcFileDateString)))
                {
                    SetStatusCode304(context.Response);
                    return true;
                }

                context.Response.ContentLength64 = fileInfo.Length;

                SetGeneralHeaders(context.Response, utcFileDateString, fileInfo.Extension);

                if (!sendBuffer)
                {
                    return true;
                }

                // If buffer is null something is really wrong
                if (buffer == null)
                {
                    return false;
                }

                await WriteFileAsync(
                        partialHeader, 
                        context.Response, 
                        buffer, 
                        context.AcceptGzip(buffer.Length),
                        ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Connection error, nothing else to do
                var isListenerException =
#if !NETSTANDARD1_3
                    ex is System.Net.HttpListenerException ||
#endif
                    ex is Net.HttpListenerException;

                if (!isListenerException)
                    throw;
            }
            finally
            {
                buffer?.Dispose();
            }

            return true;
        }

        private Stream GetFileStream(IHttpContext context, FileSystemInfo fileInfo, bool usingPartial, out bool isTagValid)
        {
            isTagValid = false;
            var localPath = fileInfo.FullName;

            if (UseRamCache && RamCache.IsValid(localPath, fileInfo.LastWriteTime, out var currentHash))
            {
                isTagValid = context.RequestHeader(HttpHeaders.IfNotMatch) == currentHash;

                if (isTagValid)
                {
                    $"RAM Cache: {localPath}".Debug(nameof(StaticFilesModule));

                    context.Response.AddHeader(HttpHeaders.ETag, currentHash);
                    return new MemoryStream(RamCache[localPath].Buffer);
                }
            }

            $"File System: {localPath}".Debug(nameof(StaticFilesModule));

            var buffer = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (usingPartial == false)
            {
                isTagValid = UpdateFileCache(
                    context.Response,
                    buffer,
                    fileInfo.LastWriteTime,
                    context.RequestHeader(HttpHeaders.IfNotMatch),
                    localPath);
            }

            return buffer;
        }

        private bool UpdateFileCache(
            IHttpResponse response,
            Stream buffer,
            DateTime fileDate,
            string requestHash,
            string localPath)
        {
            var currentHash = _fileHashCache.TryGetValue(localPath, out var currentTuple) &&
                              fileDate.Ticks == currentTuple.Item1
                ? currentTuple.Item2
                : $"{buffer.ComputeMD5().ToUpperHex()}-{fileDate.Ticks}";

            _fileHashCache.TryAdd(localPath, new Tuple<long, string>(fileDate.Ticks, currentHash));

            if (!string.IsNullOrWhiteSpace(requestHash) && requestHash == currentHash)
            {
                return true;
            }

            if (UseRamCache && buffer.Length <= MaxRamCacheFileSize)
            {
                RamCache.Add(buffer, localPath, fileDate);
            }

            response.AddHeader(HttpHeaders.ETag, currentHash);

            return false;
        }

        private void SetStatusCode304(IHttpResponse response)
        {
            SetDefaultCacheHeaders(response);

            response.ContentType = string.Empty;
            response.StatusCode = 304;
        }
    }
}
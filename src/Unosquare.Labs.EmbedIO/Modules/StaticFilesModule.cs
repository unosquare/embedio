namespace Unosquare.Labs.EmbedIO.Modules
{
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
#if NET47
    using System.Net;
#else
    using Net;
#endif
    using static VirtualPaths;

    /// <summary>
    /// Represents a simple module to server static files from the file system.
    /// </summary>
    public class StaticFilesModule
        : FileModuleBase
    {
        /// <summary>
        /// Default document constant to "index.html".
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        /// <summary>
        /// Maximal length of entry in DirectoryBrowser.
        /// </summary>
        private const int MaxEntryLength = 50;

        /// <summary>
        /// How many characters used after time in DirectoryBrowser.
        /// </summary>
        private const int SizeIndent = 20;

        private readonly VirtualPaths _virtualPaths;

        private readonly ConcurrentDictionary<string, Tuple<long, string>> _fileHashCache =
            new ConcurrentDictionary<string, Tuple<long, string>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public StaticFilesModule(Dictionary<string, string> paths)
            : this(paths.First().Value, null, paths)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="useDirectoryBrowser">if set to <c>true</c> [use directory browser].</param>
        public StaticFilesModule(string fileSystemPath, bool useDirectoryBrowser)
            : this(fileSystemPath, null, null, useDirectoryBrowser)
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
            Dictionary<string, string> headers = null,
            Dictionary<string, string> additionalPaths = null,
            bool useDirectoryBrowser = false)
        {
            if (!Directory.Exists(fileSystemPath))
                throw new ArgumentException($"Path '{fileSystemPath}' does not exist.");

            _virtualPaths = new VirtualPaths(Path.GetFullPath(fileSystemPath), useDirectoryBrowser);

            UseGzip = true;
#if DEBUG 
            // When debugging, disable RamCache
            UseRamCache = false;
#else 
            // Otherwise, enable it by default
            UseRamCache = true;
#endif
            DefaultDocument = DefaultDocumentName;

            headers?.ForEach(DefaultHeaders.Add);
            additionalPaths?.Where(path => path.Key != "/")
                .ToDictionary(x => x.Key, x => x.Value)
                .ForEach(RegisterVirtualPath);

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
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
            get => _virtualPaths.DefaultDocument;
            set => _virtualPaths.DefaultDocument = value;
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
            get => _virtualPaths.DefaultExtension;
            set => _virtualPaths.DefaultExtension = value;
        }

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        /// <value>
        /// The file system path.
        /// </value>
        public string FileSystemPath => _virtualPaths.FileSystemPath;

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
        public ReadOnlyDictionary<string, string> VirtualPaths => _virtualPaths.Collection;

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
        /// Registers the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when a method call is invalid for the object's current state.
        /// </exception>
        public void RegisterVirtualPath(string virtualPath, string physicalPath)
            => _virtualPaths.RegisterVirtualPath(virtualPath, physicalPath);

        /// <summary>
        /// Unregisters the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <exception cref="InvalidOperationException">
        /// Is thrown when a method call is invalid for the object's current state.
        /// </exception>
        public void UnregisterVirtualPath(string virtualPath) => _virtualPaths.UnregisterVirtualPath(virtualPath);

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache() => RamCache.Clear();

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
            switch (ValidatePath(context, out var requestFullLocalPath))
            {
                case VirtualPathStatus.Forbidden:
                    context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                    return Task.FromResult(true);
                case VirtualPathStatus.File:
                    return HandleFile(context, requestFullLocalPath, sendBuffer, ct);
                case VirtualPathStatus.Directory:
                    return HandleDirectory(context, requestFullLocalPath, ct);
                default:
                    return Task.FromResult(false);
            }
        }

        private async Task<bool> HandleFile(IHttpContext context, string localPath, bool sendBuffer, CancellationToken ct)
        {
            Stream buffer = null;

            try
            {
                var isTagValid = false;
                var partialHeader = context.RequestHeader(Headers.Range);
                var usingPartial = partialHeader?.StartsWith("bytes=") == true;
                var fileDate = File.GetLastWriteTime(localPath);

                if (UseRamCache && RamCache.IsValid(localPath, fileDate, out var currentHash))
                {
                    $"RAM Cache: {localPath}".Debug();

                    if (context.RequestHeader(Headers.IfNotMatch) != currentHash)
                    {
                        buffer = new MemoryStream(RamCache[localPath].Buffer);
                        context.Response.AddHeader(Headers.ETag, currentHash);
                    }
                    else
                    {
                        isTagValid = true;
                    }
                }
                else
                {
                    $"File System: {localPath}".Debug();

                    if (sendBuffer)
                    {
                        buffer = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                        if (usingPartial == false)
                        {
                            isTagValid = UpdateFileCache(
                                context.Response,
                                buffer,
                                fileDate,
                                context.RequestHeader(Headers.IfNotMatch),
                                localPath);
                        }
                    }
                }

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = fileDate.ToUniversalTime()
                    .ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo);

                if (usingPartial == false &&
                    (isTagValid || context.RequestHeader(Headers.IfModifiedSince).Equals(utcFileDateString)))
                {
                    SetStatusCode304(context.Response);
                    return true;
                }

                SetHeaders(context.Response, localPath, utcFileDateString);

                var fileSize = new FileInfo(localPath).Length;

                if (sendBuffer == false)
                {
                    context.Response.ContentLength64 = buffer?.Length ?? fileSize;
                    return true;
                }

                // If buffer is null something is really wrong
                if (buffer == null)
                {
                    return false;
                }

                await WriteFileAsync(usingPartial, partialHeader, fileSize, context, buffer, ct);
            }
            catch (HttpListenerException)
            {
                // Connection error, nothing else to do
            }
            finally
            {
                buffer?.Dispose();
            }

            return true;
        }

        private VirtualPathStatus ValidatePath(IHttpContext context, out string requestFullLocalPath)
        {
            var baseLocalPath = FileSystemPath;
            var requestLocalPath = _virtualPaths.GetUrlPath(context.RequestPathCaseSensitive(), ref baseLocalPath);

            requestFullLocalPath = Path.Combine(baseLocalPath, requestLocalPath);

            return _virtualPaths.ExistsLocalPath(requestLocalPath, ref requestFullLocalPath);
        }

        private void SetHeaders(IHttpResponse response, string localPath, string utcFileDateString)
        {
            var fileExtension = Path.GetExtension(localPath);

            if (!string.IsNullOrWhiteSpace(fileExtension) && MimeTypes.Value.ContainsKey(fileExtension))
                response.ContentType = MimeTypes.Value[fileExtension];

            SetDefaultCacheHeaders(response);

            response.AddHeader(Headers.LastModified, utcFileDateString);
            response.AddHeader(Headers.AcceptRanges, "bytes");
        }

        private bool UpdateFileCache(
            IHttpResponse response,
            Stream buffer,
            DateTime fileDate,
            string requestHash,
            string localPath)
        {
            var currentHash = _fileHashCache.TryGetValue(localPath, out var currentTuple) && fileDate.Ticks == currentTuple.Item1
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

            response.AddHeader(Headers.ETag, currentHash);

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

namespace Unosquare.Labs.EmbedIO.Modules
{
    using EmbedIO;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using Swan;
    using System.Threading;
    using System.Threading.Tasks;
#if NET46
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents a simple module to server static files from the file system.
    /// </summary>
    public class StaticFilesModule : WebModuleBase
    {
        /// <summary>
        /// The chuck size for sending files
        /// </summary>
        private const int ChuckSize = 256*1024;

        /// <summary>
        /// The maximum gzip input length
        /// </summary>
        private const int MaxGzipInputLength = 4*1024*1024;

        private readonly Dictionary<string, string> m_VirtualPaths =
            new Dictionary<string, string>(Constants.StandardStringComparer);

        private readonly Dictionary<string, string> m_MimeTypes =
            new Dictionary<string, string>(Constants.StandardStringComparer);

        /// <summary>
        /// Default document constant to "index.html"
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        /// <summary>
        /// Gets or sets the maximum size of the ram cache file.
        /// </summary>
        /// <value>
        /// The maximum size of the ram cache file.
        /// </value>
        public int MaxRamCacheFileSize { get; set; }

        /// <summary>
        /// Gets or sets the default document.
        /// Defaults to "index.html"
        /// Example: "root.xml"
        /// </summary>
        /// <value>
        /// The default document.
        /// </value>
        public string DefaultDocument { get; set; }

        /// <summary>
        /// Gets or sets the default extension.
        /// Defaults to null
        /// Example: ".html"
        /// </summary>
        /// <value>
        /// The default extension.
        /// </value>
        public string DefaultExtension { get; set; }

        /// <summary>
        /// Gets the collection holding the MIME types.
        /// </summary>
        /// <value>
        /// The MIME types.
        /// </value>
        public ReadOnlyDictionary<string, string> MimeTypes => new ReadOnlyDictionary<string, string>(m_MimeTypes);

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        /// <value>
        /// The file system path.
        /// </value>
        public string FileSystemPath { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to use the RAM Cache feature
        /// RAM Cache will only cache files that are MaxRamCacheSize in bytes or less
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use ram cache]; otherwise, <c>false</c>.
        /// </value>
        public bool UseRamCache { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use gzip].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use gzip]; otherwise, <c>false</c>.
        /// </value>
        public bool UseGzip { get; set; }

        /// <summary>
        /// The default headers
        /// </summary>
        public Dictionary<string, string> DefaultHeaders = new Dictionary<string, string>();

        /// <summary>
        /// Gets the virtual paths.
        /// </summary>
        /// <value>
        /// The virtual paths.
        /// </value>
        public ReadOnlyDictionary<string, string> VirtualPaths => new ReadOnlyDictionary<string, string>(m_VirtualPaths);

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "Static Files Module";

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache() => RamCache.Clear();

        /// <summary>
        /// Private collection holding the contents of the RAM Cache.
        /// </summary>
        /// <value>
        /// The ram cache.
        /// </value>
        private ConcurrentDictionary<string, RamCacheEntry> RamCache { get; }

        /// <summary>
        /// Represents a RAM Cache dictionary entry
        /// </summary>
        private class RamCacheEntry
        {
            public DateTime LastModified { get; set; }
            public byte[] Buffer { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule"/> class.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public StaticFilesModule(Dictionary<string, string> paths) : this(paths.First().Value, null, paths)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="headers">The headers to set in every request.</param>
        /// <param name="additionalPaths">The additional paths.</param>
        /// <exception cref="System.ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesModule(string fileSystemPath, Dictionary<string, string> headers = null,
            Dictionary<string, string> additionalPaths = null)
        {
            if (Directory.Exists(fileSystemPath) == false)
                throw new ArgumentException($"Path '{fileSystemPath}' does not exist.");

            FileSystemPath = Path.GetFullPath(fileSystemPath);
            UseGzip = true;
#if DEBUG
            // When debugging, disable RamCache
            UseRamCache = false;
#else
// Otherwise, enable it by default
            this.UseRamCache = true;
#endif
            RamCache = new ConcurrentDictionary<string, RamCacheEntry>(Constants.StaticFileStringComparer);
            MaxRamCacheFileSize = 250*1024;
            DefaultDocument = DefaultDocumentName;

            // Populate the default MIME types
            foreach (var kvp in Constants.DefaultMimeTypes)
            {
                m_MimeTypes.Add(kvp.Key, kvp.Value);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    DefaultHeaders.Add(header.Key, header.Value);
                }
            }

            if (additionalPaths != null)
            {
                foreach (var path in additionalPaths.Where(path => path.Key != "/"))
                {
                    RegisterVirtualPath(path.Key, path.Value);
                }
            }

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
        }

        private static bool IsPartOfPath(string targetPath, string basePath)
        {
            targetPath = Path.GetFullPath(targetPath).ToLowerInvariant().TrimEnd('/', '\\');
            basePath = Path.GetFullPath(basePath).ToLowerInvariant().TrimEnd('/', '\\');

            return targetPath.StartsWith(basePath);
        }

        private async Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct, bool sendBuffer = true)
        {
            var baseLocalPath = FileSystemPath;
            var requestLocalPath = GetUrlPath(context, ref baseLocalPath);

            var requestFullLocalPath = Path.Combine(baseLocalPath, requestLocalPath);

            // Check if the requested local path is part of the root File System Path
            if (IsPartOfPath(requestFullLocalPath, baseLocalPath) == false)
            {
                context.Response.StatusCode = (int)System.Net.HttpStatusCode.Forbidden;
                return true;
            }

            var eTagValid = false;
            Stream buffer = null;
            var partialHeader = context.RequestHeader(Constants.HeaderRange);
            var usingPartial = string.IsNullOrWhiteSpace(partialHeader) == false && partialHeader.StartsWith("bytes=");

            if (ExistsLocalPath(requestLocalPath, ref requestFullLocalPath) == false)
            {
                return false;
            }

            var fileDate = File.GetLastWriteTime(requestFullLocalPath);

            var requestHash = context.RequestHeader(Constants.HeaderIfNotMatch);

            if (RamCache.ContainsKey(requestFullLocalPath) && RamCache[requestFullLocalPath].LastModified == fileDate)
            {
                $"RAM Cache: {requestFullLocalPath}".Debug();

                var currentHash = RamCache[requestFullLocalPath].Buffer.ComputeMD5().ToUpperHex() + '-' + fileDate.Ticks;

                if (string.IsNullOrWhiteSpace(requestHash) || requestHash != currentHash)
                {
                    buffer = new MemoryStream(RamCache[requestFullLocalPath].Buffer);
                    context.Response.AddHeader(Constants.HeaderETag, currentHash);
                }
                else
                {
                    eTagValid = true;
                }
            }
            else
            {
                $"File System: {requestFullLocalPath}".Debug();

                if (sendBuffer)
                {
                    buffer = new FileStream(requestFullLocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                    if (usingPartial == false)
                    {
                        eTagValid = UpdateFileCache(context, buffer, fileDate, requestHash, requestFullLocalPath);
                    }
                }
            }

            // check to see if the file was modified or e-tag is the same
            var utcFileDateString = fileDate.ToUniversalTime()
                .ToString(Constants.BrowserTimeFormat, Constants.StandardCultureInfo);

            if (usingPartial == false &&
                (eTagValid || context.RequestHeader(Constants.HeaderIfModifiedSince).Equals(utcFileDateString)))
            {
                SetStatusCode304(context);
                return true;
            }

            SetHeaders(context, requestFullLocalPath, utcFileDateString);

            var fileSize = new FileInfo(requestFullLocalPath).Length;

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

            var lowerByteIndex = 0;
            var upperByteIndex = 0;
            long byteLength;
            var isPartial = usingPartial &&
                            CalculateRange(partialHeader, fileSize, out lowerByteIndex, out upperByteIndex);

            if (isPartial)
            {
                if (upperByteIndex > fileSize)
                {
                    context.Response.StatusCode = 416;
                    context.Response.AddHeader(Constants.HeaderContentRanges,
                        $"bytes */{fileSize}");

                    return true;
                }

                if (upperByteIndex == fileSize)
                {
                    byteLength = buffer.Length;
                }
                else
                {
                    byteLength = upperByteIndex - lowerByteIndex + 1;

                    context.Response.AddHeader(Constants.HeaderContentRanges,
                        $"bytes {lowerByteIndex}-{upperByteIndex}/{fileSize}");

                    context.Response.StatusCode = 206;

                    $"Opening stream {requestFullLocalPath} bytes {lowerByteIndex}-{upperByteIndex} size {byteLength}".Debug();
                }
            }
            else
            {
                if (UseGzip &&
                    context.RequestHeader(Constants.HeaderAcceptEncoding).Contains(Constants.HeaderCompressionGzip) &&
                    buffer.Length < MaxGzipInputLength &&
                    // Ignore audio/video from compression
                    context.Response.ContentType?.StartsWith("audio") == false &&
                    context.Response.ContentType?.StartsWith("video") == false)
                {
                    // Perform compression if available
                    buffer = buffer.Compress();
                    context.Response.AddHeader(Constants.HeaderContentEncoding, Constants.HeaderCompressionGzip);
                    lowerByteIndex = 0;
                }

                byteLength = buffer.Length;
            }

            context.Response.ContentLength64 = byteLength;

            try
            {
                await WriteToOutputStream(context, byteLength, buffer, lowerByteIndex, ct);
            }
            catch (HttpListenerException)
            {
                // Connection error, nothing else to do
            }
            finally
            {
#if NET452 || NET46
                buffer.Close();
#endif
                buffer.Dispose();
            }

            return true;
        }

        private static async Task WriteToOutputStream(HttpListenerContext context, long byteLength, Stream buffer,
            int lowerByteIndex, CancellationToken ct)
        {
            var streamBuffer = new byte[ChuckSize];
            var sendData = 0;
            var readBufferSize = ChuckSize;

            while (true)
            {
                if (sendData + ChuckSize > byteLength) readBufferSize = (int) (byteLength - sendData);

                buffer.Seek(lowerByteIndex + sendData, SeekOrigin.Begin);
                var read = await buffer.ReadAsync(streamBuffer, 0, readBufferSize, ct);

                if (read == 0) break;

                sendData += read;
                await context.Response.OutputStream.WriteAsync(streamBuffer, 0, readBufferSize, ct);
            }
        }

        private void SetHeaders(HttpListenerContext context, string localPath, string utcFileDateString)
        {
            var fileExtension = Path.GetExtension(localPath);

            if (MimeTypes.ContainsKey(fileExtension))
                context.Response.ContentType = MimeTypes[fileExtension];

            context.Response.AddHeader(Constants.HeaderCacheControl,
                DefaultHeaders.ContainsKey(Constants.HeaderCacheControl)
                    ? DefaultHeaders[Constants.HeaderCacheControl]
                    : "private");

            context.Response.AddHeader(Constants.HeaderPragma,
                DefaultHeaders.ContainsKey(Constants.HeaderPragma)
                    ? DefaultHeaders[Constants.HeaderPragma]
                    : string.Empty);

            context.Response.AddHeader(Constants.HeaderExpires,
                DefaultHeaders.ContainsKey(Constants.HeaderExpires)
                    ? DefaultHeaders[Constants.HeaderExpires]
                    : string.Empty);

            context.Response.AddHeader(Constants.HeaderLastModified, utcFileDateString);
            context.Response.AddHeader(Constants.HeaderAcceptRanges, "bytes");
        }

        private bool UpdateFileCache(HttpListenerContext context, Stream buffer, DateTime fileDate, string requestHash,
            string localPath)
        {
            var currentHash = buffer.ComputeMD5().ToUpperHex() + '-' + fileDate.Ticks;

            if (!string.IsNullOrWhiteSpace(requestHash) && requestHash == currentHash)
            {
                return true;
            }

            if (UseRamCache && buffer.Length <= MaxRamCacheFileSize)
            {
                using (var memoryStream = new MemoryStream())
                {
                    buffer.Position = 0;
                    buffer.CopyTo(memoryStream);

                    RamCache[localPath] = new RamCacheEntry()
                    {
                        LastModified = fileDate,
                        Buffer = memoryStream.ToArray()
                    };
                }
            }

            context.Response.AddHeader(Constants.HeaderETag, currentHash);

            return false;
        }

        private static bool CalculateRange(string partialHeader, long fileSize, out int lowerByteIndex,
            out int upperByteIndex)
        {
            lowerByteIndex = 0;
            upperByteIndex = 0;

            var range = partialHeader.Replace("bytes=", "").Split('-');

            if (range.Length == 2 && int.TryParse(range[0], out lowerByteIndex) &&
                int.TryParse(range[1], out upperByteIndex))
            {
                return true;
            }

            if ((range.Length == 2 && int.TryParse(range[0], out lowerByteIndex) &&
                 string.IsNullOrWhiteSpace(range[1])) ||
                (range.Length == 1 && int.TryParse(range[0], out lowerByteIndex)))
            {
                upperByteIndex = (int) fileSize;
                return true;
            }

            if (range.Length == 2 && string.IsNullOrWhiteSpace(range[0]) &&
                int.TryParse(range[1], out upperByteIndex))
            {
                lowerByteIndex = (int) fileSize - upperByteIndex;
                upperByteIndex = (int) fileSize;
                return true;
            }

            return false;
        }

        private bool ExistsLocalPath(string urlPath, ref string localPath)
        {
            if (string.IsNullOrWhiteSpace(DefaultExtension) == false && DefaultExtension.StartsWith(".") &&
                File.Exists(localPath) == false)
            {
                localPath += DefaultExtension;
            }

            if (File.Exists(localPath)) return true;

            if (Directory.Exists(localPath) && File.Exists(Path.Combine(localPath, DefaultDocument)))
            {
                localPath = Path.Combine(localPath, DefaultDocument);
            }
            else
            {
                // Try to fallback to root
                var rootLocalPath = Path.Combine(FileSystemPath, urlPath);

                if (File.Exists(rootLocalPath))
                {
                    localPath = rootLocalPath;
                }
                else if (Directory.Exists(rootLocalPath) && File.Exists(Path.Combine(rootLocalPath, DefaultDocument)))
                {
                    localPath = Path.Combine(rootLocalPath, DefaultDocument);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private string GetUrlPath(HttpListenerContext context, ref string baseLocalPath)
        {
            var urlPath = context.RequestPathCaseSensitive().Replace('/', Path.DirectorySeparatorChar);

            if (m_VirtualPaths.Any(x => context.RequestPathCaseSensitive().StartsWith(x.Key)))
            {
                var additionalPath =
                    m_VirtualPaths.FirstOrDefault(x => context.RequestPathCaseSensitive().StartsWith(x.Key));
                baseLocalPath = additionalPath.Value;
                urlPath = urlPath.Replace(additionalPath.Key.Replace('/', Path.DirectorySeparatorChar), "");

                if (string.IsNullOrWhiteSpace(urlPath))
                {
                    urlPath = Path.DirectorySeparatorChar.ToString();
                }
            }

            // adjust the path to see if we've got a default document
            if (urlPath.Last() == Path.DirectorySeparatorChar)
                urlPath = urlPath + DefaultDocument;

            urlPath = urlPath.TrimStart(Path.DirectorySeparatorChar);
            return urlPath;
        }

        private void SetStatusCode304(HttpListenerContext context)
        {
            context.Response.AddHeader(Constants.HeaderCacheControl,
                DefaultHeaders.ContainsKey(Constants.HeaderCacheControl)
                    ? DefaultHeaders[Constants.HeaderCacheControl]
                    : "private");

            context.Response.AddHeader(Constants.HeaderPragma,
                DefaultHeaders.ContainsKey(Constants.HeaderPragma)
                    ? DefaultHeaders[Constants.HeaderPragma]
                    : string.Empty);

            context.Response.AddHeader(Constants.HeaderExpires,
                DefaultHeaders.ContainsKey(Constants.HeaderExpires)
                    ? DefaultHeaders[Constants.HeaderExpires]
                    : string.Empty);

            context.Response.ContentType = string.Empty;
            context.Response.StatusCode = 304;
        }

        /// <summary>
        /// Registers the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        public void RegisterVirtualPath(string virtualPath, string physicalPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath) || virtualPath == "/" || virtualPath[0] != '/')
                throw new InvalidOperationException($"The virtual path {virtualPath} is invalid");

            if (m_VirtualPaths.ContainsKey(virtualPath))
                throw new InvalidOperationException($"The virtual path {virtualPath} already exists");

            if (Directory.Exists(physicalPath) == false)
                throw new InvalidOperationException($"The physical path {physicalPath} doesn't exist");

            physicalPath = Path.GetFullPath(physicalPath);
            m_VirtualPaths.Add(virtualPath, physicalPath);
        }

        /// <summary>
        /// Unregisters the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void UnregisterVirtualPath(string virtualPath)
        {
            if (m_VirtualPaths.ContainsKey(virtualPath) == false)
                throw new InvalidOperationException($"The virtual path {virtualPath} doesn't exists");

            m_VirtualPaths.Remove(virtualPath);
        }
    }
}
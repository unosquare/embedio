namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using EmbedIO;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
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

    /// <summary>
    /// Represents a simple module to server static files from the file system.
    /// </summary>
    public class StaticFilesModule : WebModuleBase
    {
        /// <summary>
        /// Default document constant to "index.html"
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        /// <summary>
        /// The chunk size for sending files
        /// </summary>
        private const int ChunkSize = 256 * 1024;

        /// <summary>
        /// The maximum gzip input length
        /// </summary>
        private const int MaxGzipInputLength = 4 * 1024 * 1024;

        private readonly VirtualPaths _virtualPaths = new VirtualPaths();

        private readonly Lazy<Dictionary<string, string>> _mimeTypes =
            new Lazy<Dictionary<string, string>>(
                () =>
                    new Dictionary<string, string>(Constants.MimeTypes.DefaultMimeTypes, Strings.StandardStringComparer));

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
        /// <param name="headers">The headers to set in every request.</param>
        /// <param name="additionalPaths">The additional paths.</param>
        /// <exception cref="System.ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesModule(
            string fileSystemPath,
            Dictionary<string, string> headers = null,
            Dictionary<string, string> additionalPaths = null)
        {
            if (Directory.Exists(fileSystemPath) == false)
                throw new ArgumentException($"Path '{fileSystemPath}' does not exist.");
            
            _virtualPaths.FileSystemPath = Path.GetFullPath(fileSystemPath);
            UseGzip = true;
#if DEBUG
            // When debugging, disable RamCache
            UseRamCache = false;
#else
// Otherwise, enable it by default
            this.UseRamCache = true;
#endif
            RamCache = new RamCache();
            MaxRamCacheFileSize = 250 * 1024;
            DefaultDocument = DefaultDocumentName;

            headers?.ForEach(DefaultHeaders.Add);
            additionalPaths?.Where(path => path.Key != "/")
                .ToDictionary(x => x.Key, x => x.Value)
                .ForEach(RegisterVirtualPath);

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
        }

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
        public string DefaultDocument
        {
            get => _virtualPaths.DefaultDocument;
            set => _virtualPaths.DefaultDocument = value;
        }

        /// <summary>
        /// Gets or sets the default extension.
        /// Defaults to null
        /// Example: ".html"
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
        /// Gets the collection holding the MIME types.
        /// </summary>
        /// <value>
        /// The MIME types.
        /// </value>
        public Lazy<ReadOnlyDictionary<string, string>> MimeTypes
            =>
                new Lazy<ReadOnlyDictionary<string, string>>(
                    () => new ReadOnlyDictionary<string, string>(_mimeTypes.Value));

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        /// <value>
        /// The file system path.
        /// </value>
        public string FileSystemPath => _virtualPaths.FileSystemPath;

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
        public Dictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the virtual paths.
        /// </summary>
        /// <value>
        /// The virtual paths.
        /// </value>
        public ReadOnlyDictionary<string, string> VirtualPaths => _virtualPaths.Collection;

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "Static Files Module";

        /// <summary>
        /// Private collection holding the contents of the RAM Cache.
        /// </summary>
        /// <value>
        /// The ram cache.
        /// </value>
        private RamCache RamCache { get; }

        /// <summary>
        /// Registers the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Is thrown when a method call is invalid for the object's current state
        /// </exception>
        public void RegisterVirtualPath(string virtualPath, string physicalPath)
            => _virtualPaths.RegisterVirtualPath(virtualPath, physicalPath);

        /// <summary>
        /// Unregisters the virtual path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Is thrown when a method call is invalid for the object's current state
        /// </exception>
        public void UnregisterVirtualPath(string virtualPath) => _virtualPaths.UnregisterVirtualPath(virtualPath);

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache() => RamCache.Clear();
        
        private static bool CalculateRange(
            string partialHeader,
            long fileSize,
            out int lowerByteIndex,
            out int upperByteIndex)
        {
            lowerByteIndex = 0;
            upperByteIndex = 0;

            var range = partialHeader.Replace("bytes=", string.Empty).Split('-');

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

        private static async Task WriteToOutputStream(
            HttpListenerResponse response,
            Stream buffer,
            int lowerByteIndex,
            CancellationToken ct)
        {
            var streamBuffer = new byte[ChunkSize];
            var sendData = 0;
            var readBufferSize = ChunkSize;

            while (true)
            {
                if (sendData + ChunkSize > response.ContentLength64) readBufferSize = (int)(response.ContentLength64 - sendData);

                buffer.Seek(lowerByteIndex + sendData, SeekOrigin.Begin);
                var read = await buffer.ReadAsync(streamBuffer, 0, readBufferSize, ct);

                if (read == 0) break;

                sendData += read;
                await response.OutputStream.WriteAsync(streamBuffer, 0, readBufferSize, ct);
            }
        }

        private async Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct, bool sendBuffer = true)
        {
            var validationResult = ValidatePath(context, out var requestFullLocalPath);

            if (validationResult.HasValue)
                return validationResult.Value;

            Stream buffer = null;

            try
            {
                var isTagValid = false;
                var partialHeader = context.RequestHeader(Headers.Range);
                var usingPartial = partialHeader?.StartsWith("bytes=") == true;
                var fileDate = File.GetLastWriteTime(requestFullLocalPath);
                
                if (UseRamCache && RamCache.IsValid(requestFullLocalPath, fileDate, out var currentHash))
                {
                    $"RAM Cache: {requestFullLocalPath}".Debug();

                    if (context.RequestHeader(Headers.IfNotMatch) != currentHash)
                    {
                        buffer = new MemoryStream(RamCache[requestFullLocalPath].Buffer);
                        context.Response.AddHeader(Headers.ETag, currentHash);
                    }
                    else
                    {
                        isTagValid = true;
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
                            isTagValid = UpdateFileCache(
                                context.Response, 
                                buffer, 
                                fileDate,
                                context.RequestHeader(Headers.IfNotMatch), 
                                requestFullLocalPath);
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

                SetHeaders(context.Response, requestFullLocalPath, utcFileDateString);

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

                if (usingPartial &&
                    CalculateRange(partialHeader, fileSize, out lowerByteIndex, out var upperByteIndex))
                {
                    if (upperByteIndex > fileSize)
                    {
                        // invalid partial request
                        context.Response.StatusCode = 416;
                        context.Response.AddHeader(Headers.ContentRanges, $"bytes */{fileSize}");

                        return true;
                    }

                    if (upperByteIndex == fileSize)
                    {
                        context.Response.ContentLength64 = buffer.Length;
                    }
                    else
                    {
                        context.Response.StatusCode = 206;
                        context.Response.ContentLength64 = upperByteIndex - lowerByteIndex + 1;

                        context.Response.AddHeader(Headers.ContentRanges,
                            $"bytes {lowerByteIndex}-{upperByteIndex}/{fileSize}");
                        
                        $"Opening stream {requestFullLocalPath} bytes {lowerByteIndex}-{upperByteIndex} size {context.Response.ContentLength64}"
                            .Debug();
                    }
                }
                else
                {
                    if (UseGzip &&
                        context.RequestHeader(Headers.AcceptEncoding).Contains(Headers.CompressionGzip) &&
                        buffer.Length < MaxGzipInputLength &&

                        // Ignore audio/video from compression
                        context.Response.ContentType?.StartsWith("audio") == false &&
                        context.Response.ContentType?.StartsWith("video") == false)
                    {
                        // Perform compression if available
                        buffer = buffer.Compress();
                        context.Response.AddHeader(Headers.ContentEncoding, Headers.CompressionGzip);
                        lowerByteIndex = 0;
                    }

                    context.Response.ContentLength64 = buffer.Length;
                }
                
                await WriteToOutputStream(context.Response, buffer, lowerByteIndex, ct);
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

        private bool? ValidatePath(HttpListenerContext context, out string requestFullLocalPath)
        {
            var baseLocalPath = FileSystemPath;
            var requestLocalPath = _virtualPaths.GetUrlPath(context.RequestPathCaseSensitive(), ref baseLocalPath);

            requestFullLocalPath = Path.Combine(baseLocalPath, requestLocalPath);

            // Check if the requested local path is part of the root File System Path
            if (_virtualPaths.IsPartOfPath(requestFullLocalPath, baseLocalPath) == false)
            {
                context.Response.StatusCode = (int) System.Net.HttpStatusCode.Forbidden;
                return true;
            }

            if (_virtualPaths.ExistsLocalPath(requestLocalPath, ref requestFullLocalPath) == false)
            {
                return false;
            }

            return null;
        }

        private void SetHeaders(HttpListenerResponse response, string localPath, string utcFileDateString)
        {
            var fileExtension = Path.GetExtension(localPath);

            if (MimeTypes.Value.ContainsKey(fileExtension))
                response.ContentType = MimeTypes.Value[fileExtension];

            response.AddHeader(Headers.CacheControl,
                DefaultHeaders.GetValueOrDefault(Headers.CacheControl, "private"));
            response.AddHeader(Headers.Pragma, DefaultHeaders.GetValueOrDefault(Headers.Pragma, string.Empty));
            response.AddHeader(Headers.Expires, DefaultHeaders.GetValueOrDefault(Headers.Expires, string.Empty));
            response.AddHeader(Headers.LastModified, utcFileDateString);
            response.AddHeader(Headers.AcceptRanges, "bytes");
        }

        private bool UpdateFileCache(
            HttpListenerResponse response,
            Stream buffer,
            DateTime fileDate,
            string requestHash,
            string localPath)
        {
            var currentHash = buffer.ComputeMD5().ToUpperHex() + '-' + fileDate.Ticks;

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
        
        private void SetStatusCode304(HttpListenerResponse response)
        {
            response.AddHeader(Headers.CacheControl,
                DefaultHeaders.GetValueOrDefault(Headers.CacheControl, "private"));
            response.AddHeader(Headers.Pragma, DefaultHeaders.GetValueOrDefault(Headers.Pragma, string.Empty));
            response.AddHeader(Headers.Expires, DefaultHeaders.GetValueOrDefault(Headers.Expires, string.Empty));

            response.ContentType = string.Empty;
            response.StatusCode = 304;
        }
    }
}
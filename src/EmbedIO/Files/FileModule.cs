using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Files.Internal;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Files
{
    /// <summary>
    /// A module serving files and directory listings from a <see cref="IFileProvider"/>.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class FileModule : WebModuleBase, IDisposable, IMimeTypeCustomizer
    {
        /// <summary>
        /// <para>Default value for <see cref="DefaultDocument"/>.</para>
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        private const string _logSource = nameof(FileModule);

        private readonly string _cacheSectionName = UniqueIdGenerator.GetNext();
        private readonly Dictionary<string, string> _customMimeTypes = new Dictionary<string, string>();
        private readonly ConcurrentDictionary<string, MappedResourceInfo> _pathCache;

        private FileCache _cache = FileCache.Default;
        private bool _contentCaching = true;
        private string _defaultDocument = DefaultDocumentName;
        private string _defaultExtension;
        private IDirectoryLister _directoryLister = Files.DirectoryLister.Default;
        private FileRequestHandlerCallback _onMappingFailed = FileRequestHandler.PassThrough;
        private FileRequestHandlerCallback _onDirectoryNotListable = FileRequestHandler.PassThrough;
        private FileRequestHandlerCallback _onMethodNotAllowed = FileRequestHandler.ThrowMethodNotAllowed;

        private FileCache.Section _cacheSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class,
        /// using the specified cache.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="provider">An <see cref="IFileProvider"/> interface that provides access
        /// to actual files and directories.</param>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <see langword="null"/>.</exception>
        public FileModule(string baseUrlPath, IFileProvider provider)
            : base(baseUrlPath)
        {
            Provider = Validate.NotNull(nameof(provider), provider);
            _pathCache = Provider.IsImmutable
                ? new ConcurrentDictionary<string, MappedResourceInfo>()
                : null;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="FileModule"/> class.
        /// </summary>
        ~FileModule()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets the <see cref="IFileProvider"/>interface that provides access
        /// to actual files and directories served by this module.
        /// </summary>
        public IFileProvider Provider { get; }

        /// <summary>
        /// Gets or sets the <see cref="FileCache"/> used by this module to store hashes and,
        /// optionally, file contents and rendered directory listings.
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        public FileCache Cache
        {
            get => _cache;
            set
            {
                EnsureConfigurationNotLocked();
                _cache = Validate.NotNull(nameof(value), value);
            }
        }

        /// <summary>
        /// <para>Gets or sets a value indicating whether this module caches the contents of files
        /// and directory listings.</para>
        /// <para>Note that the actual representations of files are stored in <see cref="FileCache"/>;
        /// thus, for example, if a file is always requested with an <c>Accept-Encoding</c> of <c>gzip</c>,
        /// only the gzipped contents of the file will be cached.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public bool ContentCaching
        {
            get => _contentCaching;
            set
            {
                EnsureConfigurationNotLocked();
                _contentCaching = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the name of the default document served, if it exists, instead of a directory listing
        /// when the path of a requested URL maps to a directory.</para>
        /// <para>The default value for this property is the <see cref="DefaultDocumentName"/> constant.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public string DefaultDocument
        {
            get => _defaultDocument;
            set
            {
                EnsureConfigurationNotLocked();
                _defaultDocument = string.IsNullOrEmpty(value) ? null : value;
            }
        }

        /// <summary>
        /// <para>Gets or sets the default extension appended to requested URL paths that do not map
        /// to any file or directory. Defaults to <see langword="null"/>.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentException">This property is being set to a non-<see langword="null"/>,
        /// non-empty string that does not start with a period (<c>.</c>).</exception>
        public string DefaultExtension
        {
            get => _defaultExtension;
            set
            {
                EnsureConfigurationNotLocked();

                if (string.IsNullOrEmpty(value))
                {
                    _defaultExtension = null;
                }
                else if (value[0] != '.')
                {
                    throw new ArgumentException("Default extension does not start with a period.", nameof(value));
                }
                else
                {
                    _defaultExtension = value;
                }
            }
        }

        /// <summary>
        /// <para>Gets or sets the <see cref="IDirectoryLister"/> interface used to generate
        /// directory listing in this module.</para>
        /// <para>A value of <see langword="null"/> disables the generation
        /// of directory listings.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public IDirectoryLister DirectoryLister
        {
            get => _directoryLister;
            set
            {
                EnsureConfigurationNotLocked();
                _directoryLister = value;
            }
        }

        /// <summary>
        /// <para>Gets or sets a <see cref="FileRequestHandlerCallback"/> that is called whenever
        /// the requested URL path could not be mapped to any file or directory.</para>
        /// <para>The default is <see cref="FileRequestHandler.PassThrough"/>.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <seealso cref="FileRequestHandler"/>
        public FileRequestHandlerCallback OnMappingFailed
        {
            get => _onMappingFailed;
            set
            {
                EnsureConfigurationNotLocked();
                _onMappingFailed = Validate.NotNull(nameof(value), value);
            }
        }

        /// <summary>
        /// <para>Gets or sets a <see cref="FileRequestHandlerCallback"/> that is called whenever
        /// the requested URL path has been mapped to a directory, but directory listing has been
        /// disabled by setting <see cref="DirectoryLister"/> to <see langword="null"/>.</para>
        /// <para>The default is <see cref="FileRequestHandler.PassThrough"/>.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <seealso cref="FileRequestHandler"/>
        public FileRequestHandlerCallback OnDirectoryNotListable
        {
            get => _onDirectoryNotListable;
            set
            {
                EnsureConfigurationNotLocked();
                _onDirectoryNotListable = Validate.NotNull(nameof(value), value);
            }
        }

        /// <summary>
        /// <para>Gets or sets a <see cref="FileRequestHandlerCallback"/> that is called whenever
        /// the requested URL path has been mapped to a file or directory, but the request's
        /// HTTP method is neither <c>GET</c> nor <c>HEAD</c>.</para>
        /// <para>The default is <see cref="FileRequestHandler.ThrowMethodNotAllowed"/>.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">This property is being set to <see langword="null"/>.</exception>
        /// <seealso cref="FileRequestHandler"/>
        public FileRequestHandlerCallback OnMethodNotAllowed
        {
            get => _onMethodNotAllowed;
            set
            {
                EnsureConfigurationNotLocked();
                _onMethodNotAllowed = Validate.NotNull(nameof(value), value);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool IMimeTypeProvider.TryGetMimeType(string extension, out string mimeType)
            => _customMimeTypes.TryGetValue(
                Validate.NotNull(nameof(extension), extension),
                out mimeType);

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="extension"/>is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="mimeType"/>is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="extension"/>is the empty string.</para>
        /// <para>- or -</para>
        /// <para><paramref name="mimeType"/>is the empty string.</para>
        /// </exception>
        public void AddCustomMimeType(string extension, string mimeType)
        {
            EnsureConfigurationNotLocked();
            _customMimeTypes[Validate.NotNullOrEmpty(nameof(extension), extension)]
                = Validate.NotNullOrEmpty(nameof(mimeType), mimeType);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            Cache.RemoveSection(_cacheSectionName);
        }

        /// <inheritdoc />
        protected override void OnStart(CancellationToken cancellationToken)
        {
            base.OnStart(cancellationToken);

            _cacheSection = Cache.AddSection(_cacheSectionName);
        }

        /// <inheritdoc />
        protected override async Task<bool> OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            MappedResourceInfo info;
            if (_pathCache != null)
            {
                if (!_pathCache.TryGetValue(path, out info))
                {
                    info = MapUrlPath(path, context);
                    if (info != null)
                        _pathCache.AddOrUpdate(path, info, (_, __) => info);
                }
            }
            else
            {
                info = MapUrlPath(path, context);
            }

            if (info == null)
                return await OnMappingFailed(context, path, null, cancellationToken).ConfigureAwait(false);

            if (!IsHttpMethodAllowed(context.Request, out var sendBuffer))
                return await OnMethodNotAllowed(context, path, info, cancellationToken).ConfigureAwait(false);

            // Exactly one of these will be non-null.
            var directoryInfo = info as MappedDirectoryInfo;
            var fileInfo = info as MappedFileInfo;

            if (directoryInfo != null && DirectoryLister == null)
                return await OnDirectoryNotListable(context, path, info, cancellationToken).ConfigureAwait(false);

            if (!context.Request.TryNegotiateContentEncoding(true, out var compressionMethod, out var prepareResponse))
            {
                prepareResponse(context.Response);
                return true;
            }

            if (!_cacheSection.TryGet(info.Path, out var cacheItem))
            {
                var contentType = fileInfo?.ContentType ?? DirectoryLister.ContentType;
                cacheItem = new FileCacheItem(_cacheSection, contentType, info.LastWriteTimeUtc);
                _cacheSection.Add(info.Path, cacheItem);
            }

            var cachingThreshold = 1024L * Cache.MaxFileSizeKb;
            var (content, entityTag) = cacheItem.GetContentAndEntityTag(compressionMethod);
            if (content == null)
            {
                var wasNotInCache = entityTag == null;
                bool cacheThisContent;
                if (directoryInfo != null)
                {
                    // It's a directory: must obtain a listing.
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var stream = new CompressionStream(memoryStream, compressionMethod))
                        {
                            await DirectoryLister.ListDirectoryAsync(
                                directoryInfo,
                                context.Request.Url.AbsolutePath,
                                Provider.GetDirectoryEntries(directoryInfo.Path, context),
                                stream,
                                cancellationToken).ConfigureAwait(false);

                            // Don't cache the listing if its uncompressed size is above the caching threshold.
                            // This is akin to checking a file's size.
                            cacheThisContent = stream.UncompressedLength <= cachingThreshold;
                        }

                        content = memoryStream.ToArray();
                    }

                    if (wasNotInCache)
                        entityTag = EntityTag.Compute(directoryInfo.LastWriteTimeUtc, content);
                }
                else if (fileInfo.Size > cachingThreshold)
                {
                    cacheThisContent = false;
                    if (wasNotInCache)
                    {
                        entityTag = await EntityTag.ComputeAsync(Provider, fileInfo, compressionMethod, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    cacheThisContent = true;
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var target = new CompressionStream(memoryStream, compressionMethod))
                        using (var source = Provider.OpenFile(fileInfo.Path))
                        {
                            await source.CopyToAsync(target, WebServer.StreamCopyBufferSize, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        content = memoryStream.ToArray();
                    }

                    if (wasNotInCache)
                        entityTag = EntityTag.Compute(fileInfo.LastWriteTimeUtc, content);
                }

                if (wasNotInCache)
                {
                    cacheItem.SetContentAndEntityTag(
                        compressionMethod,
                        cacheThisContent && ContentCaching ? content : null,
                        entityTag);
                }
            }

            /* If content is null at this point:
             *   - it's a file (we would have listed un uncached directory by now);
             *   - either it's too big for caching, or content caching is disabled.
             */

            bool SendNotModified()
            {
                var response = context.Response;
                response.SetEmptyResponse((int)HttpStatusCode.NotModified);
                prepareResponse(response);
                response.Headers.Set(HttpHeaderNames.ETag, entityTag);
                response.Headers.Set(HttpHeaderNames.LastModified, info.LastWriteTimeUtc.ToRfc1123String());

                return true;
            }


            if (context.Request.Headers.GetValues(HttpHeaderNames.IfNoneMatch)?.Select(t => t.Trim()).Contains(entityTag) ?? false)
                return SendNotModified();
        }

        private static bool IsHttpMethodAllowed(IHttpRequest request, out bool sendBuffer)
        {
            switch (request.HttpVerb)
            {
                case HttpVerbs.Head:
                    sendBuffer = false;
                    return true;
                case HttpVerbs.Get:
                    sendBuffer = true;
                    return true;
                default:
                    sendBuffer = default;
                    return false;
            }
        }

        /// <summary>
        /// Sets the default cache headers.
        /// </summary>
        /// <param name="response">The response.</param>
        private void SetDefaultCacheHeaders(IHttpResponse response)
        {
            response.Headers.Set(HttpHeaderNames.CacheControl, "private");
            response.Headers.Set(HttpHeaderNames.Expires, string.Empty);
        }

        /// <summary>
        /// Sets the general headers.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="utcFileDateString">The UTC file date string.</param>
        /// <param name="fileExtension">The file extension.</param>
        private void SetGeneralHeaders(IHttpContext context, string utcFileDateString, string fileExtension)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension) && context.TryGetMimeType(fileExtension, out var mimeType))
                context.Response.ContentType = mimeType;

            SetDefaultCacheHeaders(context.Response);

            context.Response.Headers.Set(HttpHeaderNames.LastModified, utcFileDateString);
        }

        // Tries to map a module-relative URL path to a mapped resource,
        // handling DefaultDocument and DefaultExtension.
        // Returns null if not found.
        // Directories mus be returned regardless of directory listing being enabled.
        private MappedResourceInfo MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            // If urlPath maps to a file, no further searching is needed.
            var result = Provider.MapUrlPath(urlPath, mimeTypeProvider);
            switch (result)
            {
                case MappedFileInfo _:
                    return result;

                case MappedDirectoryInfo _:
                    // Default document takes precedence over directory listing.
                    if (DefaultDocument == null)
                        return result;

                    // Look for a default document.
                    // Don't append an additional slash if the URL path is "/".
                    // The default document, if found, must be a file, not a directory.
                    var defaultDocumentResult = Provider.MapUrlPath(urlPath + (urlPath.Length < 2 ? "/" : string.Empty) + DefaultDocument, mimeTypeProvider);
                    return defaultDocumentResult as MappedFileInfo ?? result;
            }

            // Bail out if there is no default extension, or if the URL path is "/".
            if (DefaultExtension == null || urlPath.Length < 2)
                return null;

            // When the default extension is applied, the result must be a file.
            return Provider.MapUrlPath(urlPath + DefaultExtension, mimeTypeProvider) as MappedFileInfo;
        }

        private async Task<bool> HandleFileAsync(
            IHttpContext context,
            string path,
            MappedFileInfo info,
            bool sendBuffer,
            CancellationToken cancellationToken)
        {
            Stream buffer = null;

            try
            {
                var isTagValid = false;
                var partialHeader = context.Request.Headers[HttpHeaderNames.Range];
                var usingPartial = partialHeader?.StartsWith("bytes=") == true;
                var fileInfo = new FileInfo(localPath);

                if (sendBuffer)
                    buffer = GetFileStream(context, fileInfo, usingPartial, out isTagValid);

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = fileInfo.LastWriteTimeUtc.ToRfc1123String();

                if (!usingPartial
                  && (isTagValid || string.Equals(context.Request.Headers[HttpHeaderNames.IfModifiedSince], utcFileDateString)))
                {
                    SetDefaultCacheHeaders(context.Response);
                    context.Response.SetEmptyResponse((int)HttpStatusCode.NotModified);
                    return true;
                }

                context.Response.ContentLength64 = fileInfo.Length;

                SetGeneralHeaders(context, utcFileDateString, fileInfo.Extension);

                if (!sendBuffer)
                {
                    return true;
                }

                // If buffer is null something is really wrong
                if (buffer == null)
                {
                    return false;
                }

                await WriteFileAsync(partialHeader, context, buffer, cancellationToken)
                    .ConfigureAwait(false);
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

        private async Task SendFileAsync(IHttpContext context, MappedFileInfo info, CancellationToken cancellationToken = default)
        {
            var lowerByteIndex = 0L;
            var upperByteIndex = info.Size - 1;
            var partialContent = false;
            if (Provider.CanSeekFiles && RangeHeaderValue.TryParse(context.Request.Headers[HttpHeaderNames.Range], out var range))
            {
                var firstRange = range.Ranges.First();
                lowerByteIndex = firstRange.From ?? 0L;
                upperByteIndex = firstRange.To ?? info.Size - 1;

                if (upperByteIndex >= info.Size)
                {
                    context.Response.SetEmptyResponse((int)HttpStatusCode.RequestedRangeNotSatisfiable);
                    context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes */{info.Size}");
                    return;
                }

                partialContent = lowerByteIndex != 0 || upperByteIndex != info.Size - 1;
            }

            if (!partialContent)
            {
                context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes {lowerByteIndex}-{upperByteIndex}/{info.Size}");
            }
            else
            {
                context.Response.Headers.Set(HttpHeaderNames.AcceptRanges, Provider.CanSeekFiles ? "bytes" : "none");
            }

            var transferSize = upperByteIndex - lowerByteIndex + 1;
            if (transferSize > 0)
            {
                using (var fileStream = Provider.OpenFile(info.Path))
                using (var responseStream = context.OpenResponseStream())
                {
                    if (partialContent)
                        fileStream.Position = lowerByteIndex;

                    var buffer = new byte[WebServer.StreamCopyBufferSize];
                    while (transferSize > 0)
                    {
                        var bytesToRead = Math.Min(transferSize, WebServer.StreamCopyBufferSize);
                        var read = await fileStream.ReadAsync(buffer, 0, (int)bytesToRead, cancellationToken)
                            .ConfigureAwait(false);
                        await responseStream.WriteAsync(buffer, 0, read, cancellationToken)
                            .ConfigureAwait(false);
                        transferSize -= read;
                    }
                }
            }
        }
    }
}
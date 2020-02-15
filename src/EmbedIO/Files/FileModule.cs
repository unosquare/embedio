using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Files.Internal;
using EmbedIO.Internal;
using EmbedIO.Utilities;

namespace EmbedIO.Files
{
    /// <summary>
    /// A module serving files and directory listings from an <see cref="IFileProvider"/>.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public class FileModule : WebModuleBase, IDisposable, IMimeTypeCustomizer
    {
        /// <summary>
        /// <para>Default value for <see cref="DefaultDocument"/>.</para>
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        private readonly string _cacheSectionName = UniqueIdGenerator.GetNext();
        private readonly MimeTypeCustomizer _mimeTypeCustomizer = new MimeTypeCustomizer();
        private readonly ConcurrentDictionary<string, MappedResourceInfo>? _mappingCache;

        private FileCache _cache = FileCache.Default;
        private bool _contentCaching = true;
        private string? _defaultDocument = DefaultDocumentName;
        private string? _defaultExtension;
        private IDirectoryLister? _directoryLister;
        private FileRequestHandlerCallback _onMappingFailed = FileRequestHandler.ThrowNotFound;
        private FileRequestHandlerCallback _onDirectoryNotListable = FileRequestHandler.ThrowUnauthorized;
        private FileRequestHandlerCallback _onMethodNotAllowed = FileRequestHandler.ThrowMethodNotAllowed;

        private FileCache.Section? _cacheSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class,
        /// using the specified cache.
        /// </summary>
        /// <param name="baseRoute">The base route.</param>
        /// <param name="provider">An <see cref="IFileProvider"/> interface that provides access
        /// to actual files and directories.</param>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is <see langword="null"/>.</exception>
        public FileModule(string baseRoute, IFileProvider provider)
            : base(baseRoute)
        {
            Provider = Validate.NotNull(nameof(provider), provider);
            _mappingCache = Provider.IsImmutable
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

        /// <inheritdoc />
        public override bool IsFinalHandler => true;

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
        public string? DefaultDocument
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
        public string? DefaultExtension
        {
            get => _defaultExtension;
            set
            {
                EnsureConfigurationNotLocked();

                if (string.IsNullOrEmpty(value))
                {
                    _defaultExtension = null;
                }
                else if (value![0] != '.')
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
        /// <para>A value of <see langword="null"/> (the default) disables the generation
        /// of directory listings.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The module's configuration is locked.</exception>
        public IDirectoryLister? DirectoryLister
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
        /// <para>The default is <see cref="FileRequestHandler.ThrowNotFound"/>.</para>
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
        /// <para>The default is <see cref="FileRequestHandler.ThrowUnauthorized"/>.</para>
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

        string IMimeTypeProvider.GetMimeType(string extension)
            => _mimeTypeCustomizer.GetMimeType(extension);

        bool IMimeTypeProvider.TryDetermineCompression(string mimeType, out bool preferCompression)
            => _mimeTypeCustomizer.TryDetermineCompression(mimeType, out preferCompression);

        /// <inheritdoc />
        public void AddCustomMimeType(string extension, string mimeType)
            => _mimeTypeCustomizer.AddCustomMimeType(extension, mimeType);

        /// <inheritdoc />
        public void PreferCompression(string mimeType, bool preferCompression)
            => _mimeTypeCustomizer.PreferCompression(mimeType, preferCompression);

        /// <summary>
        /// Clears the part of <see cref="Cache"/> used by this module.
        /// </summary>
        public void ClearCache()
        {
            _mappingCache?.Clear();
            _cacheSection?.Clear();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources;
        /// <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (_cacheSection != null)
                Provider.ResourceChanged -= _cacheSection.Remove;

            if (Provider is IDisposable disposableProvider)
                disposableProvider.Dispose();

            if (_cacheSection != null)
                Cache.RemoveSection(_cacheSectionName);
        }

        /// <inheritdoc />
        protected override void OnBeforeLockConfiguration()
        {
            base.OnBeforeLockConfiguration();

            _mimeTypeCustomizer.Lock();
        }

        /// <inheritdoc />
        protected override void OnStart(CancellationToken cancellationToken)
        {
            base.OnStart(cancellationToken);

            _cacheSection = Cache.AddSection(_cacheSectionName);
            Provider.ResourceChanged += _cacheSection.Remove;
            Provider.Start(cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task OnRequestAsync(IHttpContext context)
        {
            MappedResourceInfo? info;

            var path = context.RequestedPath;

            // Map the URL path to a mapped resource.
            // DefaultDocument and DefaultExtension are handled here.
            // Use the mapping cache if it exists.
            if (_mappingCache == null)
            {
                info = MapUrlPath(path, context);
            }
            else if (!_mappingCache.TryGetValue(path, out info))
            {
                info = MapUrlPath(path, context);
                if (info != null)
                    _mappingCache.AddOrUpdate(path, info, (_, __) => info);
            }

            if (info == null)
            {
                // If mapping failed, send a "404 Not Found" response, or whatever OnMappingFailed chooses to do.
                // For example, it may return a default resource (think a folder of images and an imageNotFound.jpg),
                // or redirect the request.
                await OnMappingFailed(context, null).ConfigureAwait(false);
            }
            else if (!IsHttpMethodAllowed(context.Request, out var sendResponseBody))
            {
                // If there is a mapped resource, check that the HTTP method is either GET or HEAD.
                // Otherwise, send a "405 Method Not Allowed" response, or whatever OnMethodNotAllowed chooses to do.
                await OnMethodNotAllowed(context, info).ConfigureAwait(false);
            }
            else if (info.IsDirectory && DirectoryLister == null)
            {
                // If a directory listing was requested, but there is no DirectoryLister,
                // send a "403 Unauthorized" response, or whatever OnDirectoryNotListable chooses to do.
                // For example, one could prefer to send "404 Not Found" instead.
                await OnDirectoryNotListable(context, info).ConfigureAwait(false);
            }
            else
            {
                await HandleResource(context, info, sendResponseBody).ConfigureAwait(false);
            }
        }

        // Tells whether a request's HTTP method is suitable for processing by FileModule
        // and, if so, whether a response body must be sent.
        private static bool IsHttpMethodAllowed(IHttpRequest request, out bool sendResponseBody)
        {
            switch (request.HttpVerb)
            {
                case HttpVerbs.Head:
                    sendResponseBody = false;
                    return true;
                case HttpVerbs.Get:
                    sendResponseBody = true;
                    return true;
                default:
                    sendResponseBody = default;
                    return false;
            }
        }

        // Prepares response headers for a "200 OK" or "304 Not Modified" response.
        // RFC7232, Section 4.1
        private static void PreparePositiveResponse(IHttpResponse response, MappedResourceInfo info, string contentType, string entityTag, Action<IHttpResponse> setCompression)
        {
            setCompression(response);
            response.ContentType = contentType;
            response.Headers.Set(HttpHeaderNames.ETag, entityTag);
            response.Headers.Set(HttpHeaderNames.LastModified, HttpDate.Format(info.LastModifiedUtc));
            response.Headers.Set(HttpHeaderNames.CacheControl, "max-age=0, must-revalidate");
            response.Headers.Set(HttpHeaderNames.AcceptRanges, "bytes");
        }

        // Attempts to map a module-relative URL path to a mapped resource,
        // handling DefaultDocument and DefaultExtension.
        // Returns null if not found.
        // Directories mus be returned regardless of directory listing being enabled.
        private MappedResourceInfo? MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            var result = Provider.MapUrlPath(urlPath, mimeTypeProvider);

            // If urlPath maps to a file, no further searching is needed.
            if (result?.IsFile ?? false)
                return result;

            // Look for a default document.
            // Don't append an additional slash if the URL path is "/".
            // The default document, if found, must be a file, not a directory.
            if (DefaultDocument != null)
            {
                var defaultDocumentPath = urlPath + (urlPath.Length > 1 ? "/" : string.Empty) + DefaultDocument;
                var defaultDocumentResult = Provider.MapUrlPath(defaultDocumentPath, mimeTypeProvider);
                if (defaultDocumentResult?.IsFile ?? false)
                    return defaultDocumentResult;
            }

            // Try to apply default extension (but not if the URL path is "/",
            // i.e. the only normalized, non-base URL path that ends in a slash).
            // When the default extension is applied, the result must be a file.
            if (DefaultExtension != null && urlPath.Length > 1)
            {
                var defaultExtensionResult = Provider.MapUrlPath(urlPath + DefaultExtension, mimeTypeProvider);
                if (defaultExtensionResult?.IsFile ?? false)
                    return defaultExtensionResult;
            }

            return result;
        }

        private async Task HandleResource(IHttpContext context, MappedResourceInfo info, bool sendResponseBody)
        {
            // Try to extract resource information from cache.
            var cachingThreshold = 1024L * Cache.MaxFileSizeKb;
            if (!_cacheSection!.TryGet(info.Path, out var cacheItem))
            {
                // Resource information not yet cached
                cacheItem = new FileCacheItem(_cacheSection, info.LastModifiedUtc, info.Length);
                _cacheSection.Add(info.Path, cacheItem);
            }
            else if (!Provider.IsImmutable)
            {
                // Check whether the resource has changed.
                // If so, discard the cache item and create a new one.
                if (cacheItem.LastModifiedUtc != info.LastModifiedUtc || cacheItem.Length != info.Length)
                {
                    _cacheSection.Remove(info.Path);
                    cacheItem = new FileCacheItem(_cacheSection, info.LastModifiedUtc, info.Length);
                    _cacheSection.Add(info.Path, cacheItem);
                }
            }

            /*
             * Now we have a cacheItem for the resource.
             * It may have been just created, or it may or may not have a cached content,
             * depending upon the value of the ContentCaching property,
             * the size of the resource, and the value of the
             * MaxFileSizeKb of our Cache.
             */

            // If the content type is not a valid MIME type, assume the default.
            var contentType = info.ContentType ?? DirectoryLister?.ContentType ?? MimeType.Default;
            var mimeType = MimeType.StripParameters(contentType);
            if (!MimeType.IsMimeType(mimeType, false))
                contentType = mimeType = MimeType.Default;

            // Next we're going to apply proactive negotiation
            // to determine whether we agree with the client upon the compression
            // (or lack of it) to use for the resource.
            //
            // The combination of partial responses and entity compression
            // is not really standardized and could lead to a world of pain.
            // Thus, if there is a Range header in the request, try to negotiate for no compression.
            // Later, if there is compression anyway, we will ignore the Range header.
            if (!context.TryDetermineCompression(mimeType, out var preferCompression))
                preferCompression = true;
            preferCompression &= context.Request.Headers.Get(HttpHeaderNames.Range) == null;
            if (!context.Request.TryNegotiateContentEncoding(preferCompression, out var compressionMethod, out var setCompressionInResponse))
            {
                // If negotiation failed, the returned callback will do the right thing.
                setCompressionInResponse(context.Response);
                return;
            }

            var entityTag = info.GetEntityTag(compressionMethod);

            // Send a "304 Not Modified" response if applicable.
            //
            // RFC7232, Section 3.3: "A recipient MUST ignore If-Modified-Since
            //                       if the request contains an If-None-Match header field."
            if (context.Request.CheckIfNoneMatch(entityTag, out var ifNoneMatchExists)
             || (!ifNoneMatchExists && context.Request.CheckIfModifiedSince(info.LastModifiedUtc, out _)))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                PreparePositiveResponse(context.Response, info, contentType, entityTag, setCompressionInResponse);
                return;
            }

            /*
             * At this point we know the response is "200 OK",
             * unless the request is a range request.
             *
             * RFC7233, Section 3.1: "The Range header field is evaluated after evaluating the precondition
             *                       header fields defined in RFC7232, and only if the result in absence
             *                       of the Range header field would be a 200 (OK) response.  In other
             *                       words, Range is ignored when a conditional GET would result in a 304
             *                       (Not Modified) response."
             */

            // Before evaluating ranges, we must know the content length.
            // This is easy for files, as it is stored in info.Length.
            // Directories always have info.Length == 0; therefore,
            // unless the directory listing is cached, we must generate it now
            // (and cache it while we're there, if applicable).
            var content = cacheItem.GetContent(compressionMethod);
            if (info.IsDirectory && content == null)
            {
                long uncompressedLength;
                (content, uncompressedLength) = await GenerateDirectoryListingAsync(context, info, compressionMethod)
                    .ConfigureAwait(false);
                if (ContentCaching && uncompressedLength <= cachingThreshold)
                    cacheItem.SetContent(compressionMethod, content);
            }

            var contentLength = content?.Length ?? info.Length;

            // Ignore range request is compression is enabled
            // (or should I say forced, since negotiation has tried not to use it).
            var partialStart = 0L;
            var partialUpperBound = contentLength - 1;
            var isPartial = compressionMethod == CompressionMethod.None
                         && context.Request.IsRangeRequest(contentLength, entityTag, info.LastModifiedUtc, out partialStart, out partialUpperBound);
            var responseContentLength = contentLength;

            if (isPartial)
            {
                // Prepare a "206 Partial Content" response.
                responseContentLength = partialUpperBound - partialStart + 1;
                context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                PreparePositiveResponse(context.Response, info, contentType, entityTag, setCompressionInResponse);
                context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes {partialStart}-{partialUpperBound}/{contentLength}");
            }
            else
            {
                // Prepare a "200 OK" response.
                PreparePositiveResponse(context.Response, info, contentType, entityTag, setCompressionInResponse);
            }

            // If it's a HEAD request, we're done.
            if (!sendResponseBody)
                return;

            // If content must be sent AND cached, first read it and store it.
            // If the requested resource is a directory, we have already listed it by now,
            // so it must be a file for content to be null.
            if (content == null && ContentCaching && contentLength <= cachingThreshold)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var compressor = new CompressionStream(memoryStream, compressionMethod))
                    {
                        using var source = Provider.OpenFile(info.Path);
                        await source.CopyToAsync(compressor, WebServer.StreamCopyBufferSize, context.CancellationToken)
                            .ConfigureAwait(false);
                    }

                    content = memoryStream.ToArray();
                    responseContentLength = content.Length;
                }

                cacheItem.SetContent(compressionMethod, content);
            }

            // Transfer cached content if present.
            if (content != null)
            {
                context.Response.ContentLength64 = responseContentLength;
                var offset = isPartial ? (int) partialStart : 0;
                await context.Response.OutputStream.WriteAsync(content, offset, (int)responseContentLength, context.CancellationToken)
                    .ConfigureAwait(false);

                return;
            }

            // Read and transfer content without caching.
            using (var source = Provider.OpenFile(info.Path))
            {
                context.Response.SendChunked = true;

                if (isPartial)
                {
                    var buffer = new byte[WebServer.StreamCopyBufferSize];
                    if (source.CanSeek)
                    {
                        source.Position = partialStart;
                    }
                    else
                    {
                        var skipLength = (int)partialStart;
                        while (skipLength > 0)
                        {
                            var read = await source.ReadAsync(buffer, 0, Math.Min(skipLength, buffer.Length), context.CancellationToken)
                                .ConfigureAwait(false);

                            skipLength -= read;
                        }
                    }

                    var transferSize = responseContentLength;
                    while (transferSize >= WebServer.StreamCopyBufferSize)
                    {
                        var read = await source.ReadAsync(buffer, 0, WebServer.StreamCopyBufferSize, context.CancellationToken)
                            .ConfigureAwait(false);

                        await context.Response.OutputStream.WriteAsync(buffer, 0, read, context.CancellationToken)
                            .ConfigureAwait(false);

                        transferSize -= read;
                    }

                    if (transferSize > 0)
                    {
                        var read = await source.ReadAsync(buffer, 0, (int)transferSize, context.CancellationToken)
                            .ConfigureAwait(false);

                        await context.Response.OutputStream.WriteAsync(buffer, 0, read, context.CancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    using var compressor = new CompressionStream(context.Response.OutputStream, compressionMethod);
                    await source.CopyToAsync(compressor, WebServer.StreamCopyBufferSize, context.CancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        // Uses DirectoryLister to generate a directory listing asynchronously.
        // Returns a tuple of the generated content and its *uncompressed* length
        // (useful to decide whether it can be cached).
        private async Task<(byte[], long)> GenerateDirectoryListingAsync(
            IHttpContext context,
            MappedResourceInfo info,
            CompressionMethod compressionMethod)
        {
            using var memoryStream = new MemoryStream();
            using var stream = new CompressionStream(memoryStream, compressionMethod);

            await DirectoryLister!.ListDirectoryAsync(
                info,
                context.Request.Url.AbsolutePath,
                Provider.GetDirectoryEntries(info.Path, context),
                stream,
                context.CancellationToken).ConfigureAwait(false);

            return (memoryStream.ToArray(), stream.UncompressedLength);
        }
    }
}
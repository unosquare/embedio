using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
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

        private readonly string _cacheSectionName = UniqueIdGenerator.GetNext();
        private readonly Dictionary<string, string> _customMimeTypes = new Dictionary<string, string>();
        private readonly ConcurrentDictionary<string, MappedResourceInfo> _mappingCache;

        private FileCache _cache = FileCache.Default;
        private bool _contentCaching = true;
        private string _defaultDocument = DefaultDocumentName;
        private string _defaultExtension;
        private IDirectoryLister _directoryLister;
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
        /// <para>A value of <see langword="null"/> (the default) disables the generation
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

        string IMimeTypeProvider.GetMimeType(string extension)
        {
            _customMimeTypes.TryGetValue(Validate.NotNull(nameof(extension), extension), out var result);
            return result;
        }

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
            if (!disposing)
                return;

            Provider.ResourceChanged -= _cacheSection.Remove;
            if (Provider is IDisposable disposableProvider)
                disposableProvider.Dispose();

            Cache.RemoveSection(_cacheSectionName);
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
        protected override async Task<bool> OnRequestAsync(IHttpContext context, string path, CancellationToken cancellationToken)
        {
            MappedResourceInfo info;

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

            // If mapping failed, send a "404 Not Found" response, or whatever OnMappingFailed chooses to do.
            // For example, it may return a default resource (think a folder of images and an imageNotFound.jpg),
            // or redirect the request.
            if (info == null)
                return await OnMappingFailed(context, path, null, cancellationToken).ConfigureAwait(false);

            // If there is a mapped resource, check that the HTTP method is either GET or HEAD.
            // Otherwise, send a "405 Method Not Allowed" response, or whatever OnMethodNotAllowed chooses to do.
            if (!IsHttpMethodAllowed(context.Request, out var sendResponseBody))
                return await OnMethodNotAllowed(context, path, info, cancellationToken).ConfigureAwait(false);

            // If a directory listing was requested, but there is no DirectoryLister,
            // send a "403 Unauthorized" response, or whatever OnDirectoryNotListable chooses to do.
            // For example, one could prefer to send "404 Not Found" instead.
            if (info.IsDirectory && DirectoryLister == null)
                return await OnDirectoryNotListable(context, path, info, cancellationToken).ConfigureAwait(false);

            /*
             * From this point on, we know we have a legitimate resource to serve.
             */

            // Try to extract resource information from cache.
            var cachingThreshold = 1024L * Cache.MaxFileSizeKb;
            if (!_cacheSection.TryGet(info.Path, out var cacheItem))
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
             * It may have been just created (cacheItemIsNew == true);
             * otherwise it may or may not have a cached content,
             * depending upon the value of the ContentCaching property,
             * the size of the resource, and the value of the
             * MaxFileSizeKb of our Cache.
             */

            // Next we're going to apply proactive negotiation
            // to determine whether we agree with the client upon the compression
            // (or lack of it) to use for the resource.
            //
            // The combination of partial responses and entity compression
            // is not really standardized and could lead to a world of pain.
            // Thus, if there is a Range header in the request, try to negotiate for no compression.
            // Later, if there is compression anyway, we will ignore the Range header.
            var rangeHeader = context.Request.Headers.Get(HttpHeaderNames.Range);
            if (!context.Request.TryNegotiateContentEncoding(rangeHeader == null, out var compressionMethod, out var setCompressionInResponse))
            {
                // If negotiation failed, the returned callback will do the right thing.
                setCompressionInResponse(context.Response);
                return true;
            }

            /*
             * Some functions are better coded here than as private methods with a bunch of parameters.
             */

            var entityTag = info.GetEntityTag(compressionMethod);

            // Checks whether the If-None-Match request header exists
            // and specifies the right entity tag.
            // RFC7232, Section 3.2
            bool CheckIfNoneMatch(out bool headerExists)
            {
                var values = context.Request.Headers.GetValues(HttpHeaderNames.IfNoneMatch);
                if (values == null)
                {
                    headerExists = false;
                    return false;
                }

                headerExists = true;
                return values.Select(t => t.Trim()).Contains(entityTag);
            }

            // Check whether the If-Modified-Since request header exists
            // and specifies a date and time more recent than or equal to
            // the date and time of last modification of the requested resource.
            // RFC7232, Section 3.3
            bool CheckIfModifiedSince()
            {
                var value = context.Request.Headers.Get(HttpHeaderNames.IfModifiedSince);
                if (value == null || !TryParseRfc1123Date(value, out var dateTime))
                    return false;

                return dateTime >= info.LastModifiedUtc;
            }

            // Uses DirectoryLister to generate a directory listing asynchronously.
            // Returns a tuple of the generated content and its *uncompressed* length
            // (useful to decide whether it can be cached).
            async Task<(byte[], long)> GenerateDirectoryListingAsync()
            {
                using (var memoryStream = new MemoryStream())
                {
                    long uncompressedLength;
                    using (var stream = new CompressionStream(memoryStream, compressionMethod))
                    {
                        await DirectoryLister.ListDirectoryAsync(
                            info,
                            context.Request.Url.AbsolutePath,
                            Provider.GetDirectoryEntries(info.Path, context),
                            stream,
                            cancellationToken).ConfigureAwait(false);

                        uncompressedLength = stream.UncompressedLength;
                    }

                    return (memoryStream.ToArray(), uncompressedLength);
                }
            }

            // Prepares response headers for a "200 OK" or "304 Not Modified" response.
            // RFC7232, Section 4.1
            void PreparePositiveResponse(IHttpResponse response)
            {
                setCompressionInResponse(response);
                response.ContentType = info.ContentType ?? DirectoryLister.ContentType;
                response.Headers.Set(HttpHeaderNames.ETag, entityTag);
                response.Headers.Set(HttpHeaderNames.LastModified, info.LastModifiedUtc.ToRfc1123String());
                response.Headers.Set(HttpHeaderNames.CacheControl, "max-age=0, must-revalidate");
                response.Headers.Set(HttpHeaderNames.AcceptRanges, Provider.CanSeekFiles ? "bytes" : "none");
            }

            /*
             * Back to our control flow.
             */

            // Send a "304 Not Modified" response if applicable.
            //
            // RFC7232, Section 3.3: "A recipient MUST ignore If-Modified-Since
            //                       if the request contains an If-None-Match header field."
            if (CheckIfNoneMatch(out var ifNoneMatchExists) || (!ifNoneMatchExists && CheckIfModifiedSince()))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                PreparePositiveResponse(context.Response);
                return true;
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
                (content, uncompressedLength) = await GenerateDirectoryListingAsync().ConfigureAwait(false);
                if (ContentCaching && uncompressedLength <= cachingThreshold)
                    cacheItem.SetContent(compressionMethod, content);
            }

            var contentLength = content?.Length ?? info.Length;

            /*
             * More functions.
             */

            // Checks the Range request header to tell whether to send
            // a "206 Partial Content" response.
            bool IsPartial(out long start, out long upperBound)
            {
                start = 0;
                upperBound = contentLength - 1;

                // No Range header, no partial content.
                if (rangeHeader == null)
                    return false;

                // RFC7233, Section 3.1:
                // "A server MUST ignore a Range header field received with a request method other than GET."
                if (!sendResponseBody)
                    return false;

                // Ignore the Range request header if compression is enabled.
                if (compressionMethod != CompressionMethod.None)
                    return false;

                // Ignore the Range header if there is no If-Range header
                // or if the If-Range header specifies a non-matching validator.
                // RFC7233, Section 3.2: "If the validator given in the If-Range header field matches the
                //                       current validator for the selected representation of the target
                //                       resource, then the server SHOULD process the Range header field as
                //                       requested.If the validator does not match, the server MUST ignore
                //                       the Range header field.Note that this comparison by exact match,
                //                       including when the validator is an HTTP-date, differs from the
                //                       "earlier than or equal to" comparison used when evaluating an
                //                       If-Unmodified-Since conditional."
                var ifRange = context.Request.Headers.Get(HttpHeaderNames.IfRange)?.Trim();
                if (ifRange != null && ifRange != entityTag)
                {
                    if (!TryParseRfc1123Date(ifRange, out var rangeDate))
                        return false;

                    if (rangeDate != info.LastModifiedUtc)
                        return false;
                }

                // Ignore the Range request header if it cannot be parsed successfully.
                if (!RangeHeaderValue.TryParse(rangeHeader, out var range))
                    return false;

                // We can't send multipart/byteranges responses (yet),
                // thus we can only satisfy range requests that specify one range.
                if (range.Ranges.Count != 1)
                    return false;

                var firstRange = range.Ranges.First();
                start = firstRange.From ?? 0L;
                upperBound = firstRange.To ?? contentLength - 1;
                if (start >= info.Length || upperBound < start || upperBound >= info.Length)
                    throw HttpException.RangeNotSatisfiable(contentLength);

                return true;
            }

            /*
             * Back to our control flow.
             */

            var isPartial = IsPartial(out var partialStart, out var partialUpperBound);
            var partialLength = contentLength;
            if (isPartial)
            {
                // Prepare a "206 Partial Content" response.
                partialLength = partialUpperBound - partialStart + 1;
                context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                PreparePositiveResponse(context.Response);
                context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes {partialStart}-{partialUpperBound}/{contentLength}");
            }
            else
            {
                // Prepare a "200 OK" response.
                PreparePositiveResponse(context.Response);
            }

            // If it's a HEAD request, we're done.
            if (!sendResponseBody)
                return true;

            // If content must be sent AND cached, first read it and store it.
            // If the requested resource is a directory, we have already listed it by now,
            // so it must be a file for content to be null.
            if (content == null && ContentCaching && contentLength <= cachingThreshold)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var compressor = new CompressionStream(memoryStream, compressionMethod))
                    using (var source = Provider.OpenFile(info.Path))
                    {
                        await source.CopyToAsync(compressor, WebServer.StreamCopyBufferSize, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    content = memoryStream.ToArray();
                }

                cacheItem.SetContent(compressionMethod, content);
            }

            // Transfer cached content if present.
            if (content != null)
            {
                if (isPartial)
                {
                    context.Response.ContentLength64 = partialLength;
                    await context.Response.OutputStream.WriteAsync(content, (int)partialStart, (int)partialLength, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    context.Response.ContentLength64 = contentLength;
                    await context.Response.OutputStream.WriteAsync(content, 0, content.Length, cancellationToken)
                        .ConfigureAwait(false);
                }

                return true;
            }

            // Read and transfer content without caching.
            using (var source = Provider.OpenFile(info.Path))
            {
                if (isPartial)
                {
                    context.Response.ContentLength64 = partialLength;

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
                            var read = await source.ReadAsync(buffer, 0, Math.Min(skipLength, buffer.Length), cancellationToken)
                                .ConfigureAwait(false);

                            skipLength -= read;
                        }
                    }

                    var transferSize = partialLength;
                    while (transferSize >= WebServer.StreamCopyBufferSize)
                    {
                        var read = await source.ReadAsync(buffer, 0, WebServer.StreamCopyBufferSize, cancellationToken)
                            .ConfigureAwait(false);

                        await context.Response.OutputStream.WriteAsync(buffer, 0, read, cancellationToken)
                            .ConfigureAwait(false);

                        transferSize -= read;
                    }

                    if (transferSize > 0)
                    {
                        var read = await source.ReadAsync(buffer, 0, (int)transferSize, cancellationToken)
                            .ConfigureAwait(false);

                        await context.Response.OutputStream.WriteAsync(buffer, 0, read, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else
                {
                    context.Response.ContentLength64 = contentLength;
                    using (var compressor = new CompressionStream(context.Response.OutputStream, compressionMethod))
                    {
                        await source.CopyToAsync(compressor, WebServer.StreamCopyBufferSize, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }

            return true;
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

        // Attempts to parse a date and time in RFC1123 format.
        private static bool TryParseRfc1123Date(string str, out DateTime result)
            => DateTime.TryParseExact(
                str,
                "ddd, dd MMM yyyy hh: mm:ss GMT",
                CultureInfo.InvariantCulture,
                DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AssumeUniversal,
                out result);

        // Attempts to map a module-relative URL path to a mapped resource,
        // handling DefaultDocument and DefaultExtension.
        // Returns null if not found.
        // Directories mus be returned regardless of directory listing being enabled.
        private MappedResourceInfo MapUrlPath(string urlPath, IMimeTypeProvider mimeTypeProvider)
        {
            var result = Provider.MapUrlPath(urlPath, mimeTypeProvider);

            if (result != null)
            {
                // If urlPath maps to a file, no further searching is needed.
                if (result.IsFile)
                    return result;

                // Default document takes precedence over directory listing.
                if (DefaultDocument == null)
                    return result;

                // Look for a default document.
                // Don't append an additional slash if the URL path is "/".
                // The default document, if found, must be a file, not a directory.
                var defaultDocumentPath = urlPath + (urlPath.Length > 1 ? "/" : string.Empty) + DefaultDocument;
                var defaultDocumentResult = Provider.MapUrlPath(defaultDocumentPath, mimeTypeProvider);
                return defaultDocumentResult?.IsFile ?? false
                    ? defaultDocumentResult
                    : result;
            }

            // Bail out if there is no default extension, or if the URL path is "/".
            if (DefaultExtension == null || urlPath.Length < 2)
                return null;

            // When the default extension is applied, the result must be a file.
            result = Provider.MapUrlPath(urlPath + DefaultExtension, mimeTypeProvider);
            return result?.IsFile ?? false ? result : null;
        }
    }
}
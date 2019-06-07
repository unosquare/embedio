using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Files
{
    /// <summary>
    /// Represents a files module base.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public abstract class FileModuleBase : WebModuleBase, IMimeTypeCustomizer
    {
        private readonly Dictionary<string, string> _customMimeTypes = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModuleBase" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The base URL path.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        protected FileModuleBase(string baseUrlPath, bool useGzip)
            : base(baseUrlPath)
        {
            UseGzip = useGzip;
        }

        /// <summary>
        /// The default headers.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets a value indicating whether [use gzip].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use gzip]; otherwise, <c>false</c>.
        /// </value>
        public bool UseGzip { get; }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"><paramref name="extension"/>is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>This method will only look for <paramref name="extension"/> in the custom MIME type associations
        /// added on this instance using the <see cref="AddCustomMimeType"/> method.</para>
        /// <para>For a complete search in both custom (added at any level) and standard MIME types,
        /// use the <see cref="IMimeTypeProvider.TryGetMimeType">IHttpContext.TryGetMimeType</see> method.</para>
        /// </remarks>
        public bool TryGetMimeType(string extension, out string mimeType)
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
        /// Writes the file asynchronous.
        /// </summary>
        /// <param name="partialHeader">The partial header.</param>
        /// <param name="context">The context.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>
        /// A <see cref="Task" /> representing the ongoing operation.
        /// </returns>
        protected async Task WriteFileAsync(
            string partialHeader,
            IHttpContext context,
            Stream buffer,
            CancellationToken cancellationToken = default)
        {
            var fileSize = buffer.Length;

            // check if partial
            if (!CalculateRange(partialHeader, fileSize, out var lowerByteIndex, out var upperByteIndex))
            {
                using (var stream = context.OpenResponseStream())
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(stream, WebServer.StreamCopyBufferSize, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            if (upperByteIndex > fileSize)
            {
                context.Response.SetEmptyResponse((int) HttpStatusCode.RequestedRangeNotSatisfiable);
                context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes */{fileSize}");

                return;
            }

            if (lowerByteIndex != 0 || upperByteIndex != fileSize)
            {
                context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                context.Response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes {lowerByteIndex}-{upperByteIndex}/{fileSize}");
            }

            using (var stream = context.OpenResponseStream())
            {
                buffer.Position = lowerByteIndex;
                await buffer.CopyToAsync(stream, WebServer.StreamCopyBufferSize, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the default cache headers.
        /// </summary>
        /// <param name="response">The response.</param>
        protected void SetDefaultCacheHeaders(IHttpResponse response)
        {
            response.Headers.Set(HttpHeaderNames.CacheControl,
                DefaultHeaders.GetValueOrDefault(HttpHeaderNames.CacheControl, "private"));
            response.Headers.Add(HttpHeaderNames.Pragma, DefaultHeaders.GetValueOrDefault(HttpHeaderNames.Pragma, string.Empty));
            response.Headers.Set(HttpHeaderNames.Expires, DefaultHeaders.GetValueOrDefault(HttpHeaderNames.Expires, string.Empty));
        }

        /// <summary>
        /// Sets the general headers.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="utcFileDateString">The UTC file date string.</param>
        /// <param name="fileExtension">The file extension.</param>
        protected void SetGeneralHeaders(IHttpContext context, string utcFileDateString, string fileExtension)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension) && context.TryGetMimeType(fileExtension, out var mimeType))
                context.Response.ContentType = mimeType;

            SetDefaultCacheHeaders(context.Response);

            context.Response.Headers.Set(HttpHeaderNames.LastModified, utcFileDateString);
            context.Response.Headers.Set(HttpHeaderNames.AcceptRanges, "bytes");
        }

        private static bool CalculateRange(string partialHeader, long fileSize, out long lowerByteIndex, out long upperByteIndex)
        {
            lowerByteIndex = 0;
            upperByteIndex = fileSize - 1;

            if (string.IsNullOrWhiteSpace(partialHeader) || !RangeHeaderValue.TryParse(partialHeader, out var range))
                return false;

            var firstRange = range.Ranges.First();
            lowerByteIndex = firstRange.From ?? 0;
            upperByteIndex = firstRange.To ?? fileSize - 1;
            return true;
        }
    }
}
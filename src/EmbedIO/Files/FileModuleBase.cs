using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unosquare.Swan;

namespace EmbedIO.Files
{
    /// <summary>
    /// Represents a files module base.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public abstract class FileModuleBase
        : WebModuleBase
    {
        internal const int MaxGzipInputLength = 4 * 1024 * 1024;

        internal const int ChunkSize = 256 * 1024;

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

        /// <summary>
        /// Writes the file asynchronous.
        /// </summary>
        /// <param name="partialHeader">The partial header.</param>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns></returns>
        protected Task WriteFileAsync(
            string partialHeader,
            IHttpResponse response,
            Stream buffer,
            bool useGzip = true,
            CancellationToken cancellationToken = default)
        {
            var fileSize = buffer.Length;
            
            // check if partial
            if (!CalculateRange(partialHeader, fileSize, out var lowerByteIndex, out var upperByteIndex))
                return response.SendStreamAsync(buffer, UseGzip && useGzip, cancellationToken);

            if (upperByteIndex > fileSize)
            {
                // invalid partial request
                response.StatusCode = 416;
                response.ContentLength64 = 0;
                response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes */{fileSize}");

                return Task.Delay(0, cancellationToken);
            }

            if (lowerByteIndex != 0 || upperByteIndex != fileSize)
            {
                response.StatusCode = 206;
                response.ContentLength64 = upperByteIndex - lowerByteIndex + 1;

                response.Headers.Set(HttpHeaderNames.ContentRange, $"bytes {lowerByteIndex}-{upperByteIndex}/{fileSize}");
            }

            return response.SendStreamAsync(buffer, lowerByteIndex, UseGzip && useGzip, cancellationToken);
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
        /// <param name="response">The response.</param>
        /// <param name="utcFileDateString">The UTC file date string.</param>
        /// <param name="fileExtension">The file extension.</param>
        protected void SetGeneralHeaders(IHttpResponse response, string utcFileDateString, string fileExtension)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension) && MimeTypes.Associations.TryGetValue(fileExtension, out var mimeType))
                response.ContentType = mimeType;

            SetDefaultCacheHeaders(response);

            response.Headers.Set(HttpHeaderNames.LastModified, utcFileDateString);
            response.Headers.Set(HttpHeaderNames.AcceptRanges, "bytes");
        }

        private static bool CalculateRange(string partialHeader, long fileSize, out long lowerByteIndex, out long upperByteIndex)
        {
            lowerByteIndex = 0;
            upperByteIndex = fileSize - 1;

            if (string.IsNullOrWhiteSpace(partialHeader)) return false;

            try
            {
                var range = System.Net.Http.Headers.RangeHeaderValue.Parse(partialHeader).Ranges.First();
                lowerByteIndex = range.From ?? 0;
                upperByteIndex = range.To ?? fileSize - 1;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
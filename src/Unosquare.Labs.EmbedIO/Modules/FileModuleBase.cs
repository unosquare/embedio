namespace Unosquare.Labs.EmbedIO.Modules
{
    using Constants;
    using Swan;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a files module base.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public abstract class FileModuleBase
        : WebModuleBase
    {
        internal static readonly int MaxGzipInputLength = 4 * 1024 * 1024;

        internal static readonly int ChunkSize = 256 * 1024;

        /// <summary>
        /// Gets the collection holding the MIME types.
        /// </summary>
        /// <value>
        /// The MIME types.
        /// </value>
        public Lazy<ReadOnlyDictionary<string, string>> MimeTypes
            =>
                new Lazy<ReadOnlyDictionary<string, string>>(
                    () => new ReadOnlyDictionary<string, string>(Constants.MimeTypes.DefaultMimeTypes.Value));

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
        public bool UseGzip { get; set; }

        /// <summary>
        /// Writes the file asynchronous.
        /// </summary>
        /// <param name="partialHeader">The partial header.</param>
        /// <param name="response">The response.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="useGzip">if set to <c>true</c> [use gzip].</param>
        /// <param name="ct">The ct.</param>
        /// <returns></returns>
        protected Task WriteFileAsync(
            string partialHeader,
            IHttpResponse response,
            Stream buffer,
            bool useGzip = true,
            CancellationToken ct = default)
        {
            var fileSize = buffer.Length;
            
            // check if partial
            if (!CalculateRange(partialHeader, fileSize, out var lowerByteIndex, out var upperByteIndex))
                return response.BinaryResponseAsync(buffer, UseGzip && useGzip, ct);

            if (upperByteIndex > fileSize)
            {
                // invalid partial request
                response.StatusCode = 416;
                response.ContentLength64 = 0;
                response.AddHeader(HttpHeaders.ContentRanges, $"bytes */{fileSize}");

                return Task.Delay(0, ct);
            }

            if (lowerByteIndex != 0 || upperByteIndex != fileSize)
            {
                response.StatusCode = 206;
                response.ContentLength64 = upperByteIndex - lowerByteIndex + 1;

                response.AddHeader(HttpHeaders.ContentRanges,
                    $"bytes {lowerByteIndex}-{upperByteIndex}/{fileSize}");
            }

            return response.WriteToOutputStream(buffer, lowerByteIndex, ct);
        }

        /// <summary>
        /// Sets the default cache headers.
        /// </summary>
        /// <param name="response">The response.</param>
        protected void SetDefaultCacheHeaders(IHttpResponse response)
        {
            response.AddHeader(HttpHeaders.CacheControl,
                DefaultHeaders.GetValueOrDefault(HttpHeaders.CacheControl, "private"));
            response.AddHeader(HttpHeaders.Pragma, DefaultHeaders.GetValueOrDefault(HttpHeaders.Pragma, string.Empty));
            response.AddHeader(HttpHeaders.Expires, DefaultHeaders.GetValueOrDefault(HttpHeaders.Expires, string.Empty));
        }

        /// <summary>
        /// Sets the general headers.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="utcFileDateString">The UTC file date string.</param>
        /// <param name="fileExtension">The file extension.</param>
        protected void SetGeneralHeaders(IHttpResponse response, string utcFileDateString, string fileExtension)
        {
            if (!string.IsNullOrWhiteSpace(fileExtension) && MimeTypes.Value.ContainsKey(fileExtension))
                response.ContentType = MimeTypes.Value[fileExtension];

            SetDefaultCacheHeaders(response);

            response.AddHeader(HttpHeaders.LastModified, utcFileDateString);
            response.AddHeader(HttpHeaders.AcceptRanges, "bytes");
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

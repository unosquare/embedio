namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using Swan;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;

    /// <summary>
    /// Represents a files module base.
    /// </summary>
    /// <seealso cref="WebModuleBase" />
    public abstract class FileModuleBase
        : WebModuleBase
    {
        /// <summary>
        /// The maximum gzip input length.
        /// </summary>
        public static int MaxGzipInputLength = 4 * 1024 * 1024;

        /// <summary>
        /// The chunk size for sending files.
        /// </summary>
        public static int ChunkSize = 256 * 1024;

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
        /// <param name="usingPartial">if set to <c>true</c> [using partial].</param>
        /// <param name="partialHeader">The partial header.</param>
        /// <param name="fileSize">Size of the file.</param>
        /// <param name="context">The context.</param>
        /// <param name="buffer">The buffer.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task representing the write action.</returns>
        protected Task WriteFileAsync(
            bool usingPartial,
            string partialHeader,
            long fileSize,
            IHttpContext context,
            Stream buffer,
            CancellationToken ct)
        {
            // check if partial
            if (!usingPartial ||
                !CalculateRange(partialHeader, fileSize, out var lowerByteIndex, out var upperByteIndex))
                return context.BinaryResponseAsync(buffer, ct, UseGzip);

            if (upperByteIndex > fileSize)
            {
                // invalid partial request
                context.Response.StatusCode = 416;
                context.Response.AddHeader(Headers.ContentRanges, $"bytes */{fileSize}");

                return Task.Delay(0, ct);
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
            }

            return context.Response.WriteToOutputStream(buffer, lowerByteIndex, ct);

        }

        /// <summary>
        /// Sets the default cache headers.
        /// </summary>
        /// <param name="response">The response.</param>
        protected void SetDefaultCacheHeaders(IHttpResponse response)
        {
            response.AddHeader(Headers.CacheControl,
                DefaultHeaders.GetValueOrDefault(Headers.CacheControl, "private"));
            response.AddHeader(Headers.Pragma, DefaultHeaders.GetValueOrDefault(Headers.Pragma, string.Empty));
            response.AddHeader(Headers.Expires, DefaultHeaders.GetValueOrDefault(Headers.Expires, string.Empty));
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

            response.AddHeader(Headers.LastModified, utcFileDateString);
            response.AddHeader(Headers.AcceptRanges, "bytes");
        }

        private static bool CalculateRange(
            string partialHeader,
            long fileSize,
            out long lowerByteIndex,
            out long upperByteIndex)
        {
            lowerByteIndex = 0;
            upperByteIndex = 0;

            var range = partialHeader.Replace("bytes=", string.Empty).Split('-');

            if (range.Length == 2 && long.TryParse(range[0], out lowerByteIndex) &&
                long.TryParse(range[1], out upperByteIndex))
            {
                return true;
            }

            if ((range.Length == 2 && long.TryParse(range[0], out lowerByteIndex) &&
                 string.IsNullOrWhiteSpace(range[1])) ||
                (range.Length == 1 && long.TryParse(range[0], out lowerByteIndex)))
            {
                upperByteIndex = (int)fileSize;
                return true;
            }

            if (range.Length == 2 && string.IsNullOrWhiteSpace(range[0]) &&
                long.TryParse(range[1], out upperByteIndex))
            {
                lowerByteIndex = (int)fileSize - upperByteIndex;
                upperByteIndex = (int)fileSize;
                return true;
            }

            return false;
        }
    }
}

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
#if NET47
    using System.Net;
#else
    using Net;
#endif

    public abstract class FileModuleBase
        : WebModuleBase
    {
        /// <summary>
        /// The maximum gzip input length.
        /// </summary>
        protected const int MaxGzipInputLength = 4 * 1024 * 1024;

        /// <summary>
        /// The chunk size for sending files.
        /// </summary>
        private const int ChunkSize = 256 * 1024;

        private readonly Lazy<Dictionary<string, string>> _mimeTypes =
            new Lazy<Dictionary<string, string>>(
                () =>
                    new Dictionary<string, string>(Constants.MimeTypes.DefaultMimeTypes, Strings.StandardStringComparer));

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
        /// <param name="ct">The ct.</param>
        /// <returns>A task representing the write action.</returns>
        protected Task WriteFileAsync(
            bool usingPartial,
            string partialHeader,
            long fileSize,
            IHttpContext context,
            Stream buffer,
            CancellationToken ct)
        {
            long lowerByteIndex = 0;

            if (usingPartial &&
                CalculateRange(partialHeader, fileSize, out lowerByteIndex, out var upperByteIndex))
            {
                if (upperByteIndex > fileSize)
                {
                    // invalid partial request
                    context.Response.StatusCode = 416;
                    context.Response.AddHeader(Headers.ContentRanges, $"bytes */{fileSize}");

                    return Task.FromResult(0);
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

            return WriteToOutputStream(context.Response, buffer, lowerByteIndex, ct);
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

        private static async Task WriteToOutputStream(
            IHttpResponse response,
            Stream buffer,
            long lowerByteIndex,
            CancellationToken ct)
        {
            var streamBuffer = new byte[ChunkSize];
            long sendData = 0;
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

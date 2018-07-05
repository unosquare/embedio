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
    using System.Reflection;
#if NET47
    using System.Net;
#else
    using Net;
#endif

    /// <summary>
    /// Represents a simple module to server resource files from the .NET assembly.
    /// </summary>
    public class ResourceFilesModule
        : WebModuleBase
    {
        /// <summary>
        /// The chunk size for sending files
        /// </summary>
        private const int ChunkSize = 256 * 1024;

        /// <summary>
        /// The maximum gzip input length
        /// </summary>
        private const int MaxGzipInputLength = 4 * 1024 * 1024;

        private Assembly SourceAssembly;
        private string ResourcePathRoot;

        private readonly Lazy<Dictionary<string, string>> _mimeTypes =
            new Lazy<Dictionary<string, string>>(
                () =>
                    new Dictionary<string, string>(Constants.MimeTypes.DefaultMimeTypes, Strings.StandardStringComparer));

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFilesModule" /> class.
        /// </summary>
        /// <param name="sourceAssembly">The source assembly.</param>
        /// <param name="resourcePath">The resource path.</param>
        /// <exception cref="ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public ResourceFilesModule(
            Assembly sourceAssembly,
            string resourcePath)
        {
            if (sourceAssembly == null)
                throw new ArgumentNullException(nameof(sourceAssembly));

            if (sourceAssembly.GetName() == null)
                throw new ArgumentException($"Assembly '{sourceAssembly}' not valid.");

            SourceAssembly = sourceAssembly;
            ResourcePathRoot = resourcePath;

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, ct, false));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
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
        /// The default headers
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; } = new Dictionary<string, string>();

          /// <inheritdoc />
        public override string Name => nameof(ResourceFilesModule).Humanize();

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
                upperByteIndex = (int) fileSize;
                return true;
            }

            if (range.Length == 2 && string.IsNullOrWhiteSpace(range[0]) &&
                long.TryParse(range[1], out upperByteIndex))
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

        private Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct, bool sendBuffer = true)
        {
            return HandleFile(context, PathResourcerise(context.RequestPathCaseSensitive()), sendBuffer, ct);
        }

        private static string PathResourcerise(string s) => s == "/" ? "index.html" : s.Substring(1, s.Length - 1).Replace('/', '.');

        private async Task<bool> HandleFile(HttpListenerContext context, string localPath, bool sendBuffer, CancellationToken ct)
        {
            Stream buffer = null;
            try
            {
                var partialHeader = context.RequestHeader(Headers.Range);
                var usingPartial = partialHeader?.StartsWith("bytes=") == true;

                $"Resource System: {localPath}".Debug();

                buffer = SourceAssembly.GetManifestResourceStream($"{ResourcePathRoot}.{localPath}");
                
                // If buffer is null something is really wrong
                if (buffer == null)
                {
                    return false;
                }

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = DateTime.Now.ToUniversalTime()
                    .ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo);

                SetHeaders(context.Response, localPath, utcFileDateString);

                var fileSize = buffer.Length;

                // HEAD (file size only)
                if (sendBuffer == false)
                {
                    context.Response.ContentLength64 = fileSize;
                    return true;
                }

                long lowerByteIndex = 0;

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

                        $"Opening stream {localPath} bytes {lowerByteIndex}-{upperByteIndex} size {context.Response.ContentLength64}"
                            .Debug();
                    }
                }
                else
                {
                    if (context.RequestHeader(Headers.AcceptEncoding).Contains(Headers.CompressionGzip) &&
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
                }

                context.Response.ContentLength64 = buffer.Length;

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

        private void SetHeaders(HttpListenerResponse response, string localPath, string utcFileDateString)
        {
            var fileExtension = ".html";

            if (localPath.Contains("."))
                fileExtension = $".{localPath.Split('.').Last()}";

            if (MimeTypes.Value.ContainsKey(fileExtension))
                response.ContentType = MimeTypes.Value[fileExtension];

            SetDefaultCacheHeaders(response);

            response.AddHeader(Headers.LastModified, utcFileDateString);
            response.AddHeader(Headers.AcceptRanges, "bytes");
        }

        private void SetDefaultCacheHeaders(HttpListenerResponse response)
        {
            response.AddHeader(Headers.CacheControl,
                DefaultHeaders.GetValueOrDefault(Headers.CacheControl, "private"));
            response.AddHeader(Headers.Pragma, DefaultHeaders.GetValueOrDefault(Headers.Pragma, string.Empty));
            response.AddHeader(Headers.Expires, DefaultHeaders.GetValueOrDefault(Headers.Expires, string.Empty));
        }
    }
}
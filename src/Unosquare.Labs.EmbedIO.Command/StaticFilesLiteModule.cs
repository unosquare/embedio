namespace Unosquare.Labs.EmbedIO.Command
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Labs.EmbedIO.Constants;
    using Unosquare.Swan;

    class StaticFilesLiteModule
        : WebModuleBase
    {
        public override string Name => nameof(StaticFilesLiteModule).Humanize();

        /// <summary>
        /// The chunk size for sending files
        /// </summary>
        private const int ChunkSize = 256 * 1024;

        /// <summary>
        /// Maximal length of entry in DirectoryBrowser
        /// </summary>
        private const int MaxEntryLength = 50;

        /// <summary>
        /// How many characters used after time in DirectoryBrowser
        /// </summary>
        private const int SizeIndent = 20;

        private const string DefaultDocument = "index.html";

        private string FullPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="headers">The headers to set in every request.</param>
        /// <param name="additionalPaths">The additional paths.</param>
        /// <param name="useDirectoryBrowser">if set to <c>true</c> [use directory browser].</param>
        /// <exception cref="ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesLiteModule(
            string fileSystemPath,
            bool useDirectoryBrowser = false)
        {
            if (!Directory.Exists(fileSystemPath))
                throw new ArgumentException($"Path '{fileSystemPath}' does not exist.");

            FullPath = Path.GetFullPath(fileSystemPath);
            // It's need it?
            // DefaultDocument = DefaultDocumentName;

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, ct));
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

        private static Task<bool> HandleDirectory(HttpListenerContext context, string localPath, CancellationToken ct)
        {
            "Handdle Directory".WriteLine(ConsoleColor.Yellow);

            var entries = new[] { context.Request.RawUrl == "/" ? string.Empty : "<a href='../'>../</a>" }
                .Concat(
                    Directory.GetDirectories(localPath)
                        .Select(path =>
                        {
                            var name = path.Replace(
                                localPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
                                string.Empty);
                            return new
                            {
                                Name = (name + Path.DirectorySeparatorChar).Truncate(MaxEntryLength, "..>"),
                                Url = Uri.EscapeDataString(name) + Path.DirectorySeparatorChar,
                                ModificationTime = new DirectoryInfo(path).LastWriteTimeUtc,
                                Size = "-"
                            };
                        })
                        .OrderBy(x => x.Name)
                        .Union(Directory.GetFiles(localPath, "*", SearchOption.TopDirectoryOnly)
                            .Select(path =>
                            {
                                var fileInfo = new FileInfo(path);
                                var name = Path.GetFileName(path);

                                return new
                                {
                                    Name = name.Truncate(MaxEntryLength, "..>"),
                                    Url = Uri.EscapeDataString(name),
                                    ModificationTime = fileInfo.LastWriteTimeUtc,
                                    Size = fileInfo.Length.FormatBytes()
                                };
                            })
                            .OrderBy(x => x.Name))
                        .Select(y => $"<a href='{y.Url}'>{WebUtility.HtmlEncode(y.Name)}</a>" +
                                     new string(' ', MaxEntryLength - y.Name.Length + 1) +
                                     y.ModificationTime.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                                         CultureInfo.InvariantCulture) +
                                     new string(' ', SizeIndent - y.Size.Length) +
                                     y.Size))
                .Where(x => !string.IsNullOrWhiteSpace(x));

            var content = "<html><head></head><body>{0}</body></html>".Replace(
                "{0}",
                $"<h1>Index of {WebUtility.HtmlEncode(context.RequestPathCaseSensitive())}</h1><hr/><pre>{string.Join("\n", entries)}</pre><hr/>");

            return context.HtmlResponseAsync(content, cancellationToken: ct);
        }

        private Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct, bool sendBuffer = true)
        {
            var index = Path.Combine(FullPath, DefaultDocument);

            if (File.Exists(index))
                return HandleFile(context, index, sendBuffer, ct);
            else
                return HandleDirectory(context, FullPath, ct);
        }

        private async Task<bool> HandleFile(HttpListenerContext context, string localPath, bool sendBuffer, CancellationToken ct)
        {
            Stream buffer = null;

            try
            {
                var partialHeader = context.RequestHeader(Headers.Range);
                var fileDate = File.GetLastWriteTime(localPath);

                $"File System: {localPath}".Debug();

                if (sendBuffer)
                {
                    buffer = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = fileDate.ToUniversalTime()
                    .ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", new CultureInfo("en-US"));

                var fileSize = new FileInfo(localPath).Length;

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

                long lowerByteIndex = 0;

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
    }
}

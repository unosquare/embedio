namespace Unosquare.Labs.EmbedIO.Command
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Constants;
    using Swan;

    class StaticFilesLiteModule
        : WebModuleBase
    {
        private readonly Lazy<Dictionary<string, string>> _mimeTypes =
            new Lazy<Dictionary<string, string>>(
                () =>
                    new Dictionary<string, string>(Constants.MimeTypes.DefaultMimeTypes, StringComparer.InvariantCultureIgnoreCase));

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

        private readonly string _fullPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesLiteModule"/> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <exception cref="ArgumentException"></exception>
        public StaticFilesLiteModule(string fileSystemPath)
        {
            if (!Directory.Exists(fileSystemPath))
                throw new ArgumentException($"Path '{fileSystemPath}' does not exist.");

            _fullPath = Path.GetFullPath(fileSystemPath);

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, HandleGet);
        }
        
        public override string Name => nameof(StaticFilesLiteModule).Humanize();

        private static async Task WriteToOutputStream(
            HttpListenerResponse response,
            Stream buffer,
            CancellationToken ct)
        {
            var streamBuffer = new byte[ChunkSize];
            long sendData = 0;
            var readBufferSize = ChunkSize;

            while (true)
            {
                if (sendData + ChunkSize > response.ContentLength64) readBufferSize = (int)(response.ContentLength64 - sendData);

                buffer.Seek(sendData, SeekOrigin.Begin);
                var read = await buffer.ReadAsync(streamBuffer, 0, readBufferSize, ct);

                if (read == 0) break;

                sendData += read;
                await response.OutputStream.WriteAsync(streamBuffer, 0, readBufferSize, ct);
            }
        }

        private static Task<bool> HandleDirectory(HttpListenerContext context, string localPath, CancellationToken ct)
        {
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

        private Task<bool> HandleGet(HttpListenerContext context, CancellationToken ct)
        {
            var urlPath = context.Request.Url.LocalPath.Replace('/', Path.DirectorySeparatorChar);
            var basePath = Path.Combine(_fullPath, urlPath.TrimStart(new[] { Path.DirectorySeparatorChar }));

            if (urlPath.Last() == Path.DirectorySeparatorChar)
                urlPath = urlPath + DefaultDocument;

            urlPath = urlPath.TrimStart(new[] { Path.DirectorySeparatorChar });

            var path = Path.Combine(_fullPath, urlPath);
            
            if (File.Exists(path))
                return HandleFile(context, path, ct);
            
            return Directory.Exists(basePath) ? HandleDirectory(context, basePath, ct) : Task.FromResult(false);
        }

        private async Task<bool> HandleFile(HttpListenerContext context, string localPath, CancellationToken ct)
        {
            Stream buffer = null;

            try
            {
                var fileExtension = Path.GetExtension(localPath);

                if (_mimeTypes.Value.ContainsKey(fileExtension))
                    context.Response.ContentType = _mimeTypes.Value[fileExtension];

                buffer = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
 
                if (Path.GetExtension(localPath).Equals(".html") || Path.GetExtension(localPath).Equals(".htm"))
                    buffer = WriteJsWebSocket(localPath);


                context.Response.ContentLength64 = buffer.Length;

                await WriteToOutputStream(context.Response, buffer, ct);
                
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

        private static Stream WriteJsWebSocket(string path)
        {
            var file = File.ReadAllText(path, Encoding.UTF8);
            var jsTag = "<script>var ws=new WebSocket('ws://'+document.location.hostname+':"+ Program.WsPort + "/watcher');ws.onmessage=function(){document.location.reload()};</script>";
            var newFile = file.Insert(file.IndexOf("</body>"), jsTag);

            return new MemoryStream(Encoding.UTF8.GetBytes(newFile));
        }
    }
}

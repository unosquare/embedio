using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Constants;
using EmbedIO.Internal;
using EmbedIO.Utilities;
using Unosquare.Swan;

namespace EmbedIO.Modules
{
    /// <summary>
    /// Represents a simple module to serve files and directories from the file system.
    /// </summary>
    public class StaticFilesModule : FileModuleBase
    {
        /// <summary>
        /// Default document constant to "index.html".
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        /// <summary>
        /// Maximal length of entry in DirectoryBrowser.
        /// </summary>
        private const int MaxEntryLength = 50;

        /// <summary>
        /// How many characters used after time in DirectoryBrowser.
        /// </summary>
        private const int SizeIndent = 20;

        private readonly ConcurrentDictionary<string, (PathMappingResult Result, string LocalPath)> _pathCache = new ConcurrentDictionary<string, (PathMappingResult, string)>();

        private readonly ConcurrentDictionary<string, (long DateTicks, string HashCode)> _fileHashCache = new ConcurrentDictionary<string, (long, string)>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="baseUrlPath">The URL path under which file are mapped.</param>
        /// <param name="fileSystemPath">The file system path from which files are retrieved.</param>
        /// <param name="fileCachingMode">The file caching mode.</param>
        /// <param name="defaultDocument">The default document name.</param>
        /// <param name="defaultExtension">The default document extension.</param>
        /// <param name="useDirectoryBrowser">If set to <see langword="true"/>, enable directory browsing.</param>
        /// <param name="useGzip">If set to <see langword="true"/>, enable GZip compression.</param>
        /// <exception cref="ArgumentNullException">
        /// <para><paramref name="fileSystemPath"/> is <see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="baseUrlPath"/> is <see langword="null"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="fileSystemPath"/> is not a valid local path.</para>
        /// <para>- or -</para>
        /// <para><paramref name="baseUrlPath"/> is not a valid base URL path.</para>
        /// <para>- or -</para>
        /// <para><paramref name="fileCachingMode"/> has an invalid value.</para>
        /// <para>- or -</para>
        /// <para><paramref name="defaultDocument"/> contains one or more invalid characters.</para>
        /// <para>- or -</para>
        /// <para><paramref name="defaultExtension"/> does not start with a dot, or contains one or more invalid characters.</para>
        /// </exception>
        public StaticFilesModule(
            string baseUrlPath,
            string fileSystemPath,
            FileCachingMode fileCachingMode = FileCachingMode.MappingOnly,
            string defaultDocument = DefaultDocumentName,
            string defaultExtension = null,
            bool useDirectoryBrowser = false,
            bool useGzip = true)
        : base(useGzip)
        {
            BaseUrlPath = Validate.UrlPath(nameof(baseUrlPath), baseUrlPath, true);
            FileSystemPath = Validate.LocalPath(nameof(fileSystemPath), fileSystemPath, true);
            FileCachingMode = Validate.EnumValue<FileCachingMode>(nameof(fileCachingMode), fileCachingMode);
            DefaultDocument = ValidateDefaultDocument(nameof(defaultDocument), defaultDocument);
            DefaultExtension = ValidateDefaultExtension(nameof(defaultExtension), defaultExtension);
            UseDirectoryBrowser = useDirectoryBrowser;

#if DEBUG
            // When debugging, disable RamCache
            if (FileCachingMode == FileCachingMode.Complete)
                FileCachingMode = FileCachingMode.MappingOnly;
#endif

            AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (context, ct) => HandleGet(context, false, ct));
            AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (context, ct) => HandleGet(context, true, ct));
        }

        /// <summary>
        /// Gets the URL path under which files are mapped.
        /// </summary>
        /// <value>
        /// The base URL path.
        /// </value>
        public string BaseUrlPath { get; }

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        /// <value>
        /// The file system path.
        /// </value>
        public string FileSystemPath { get; }

        /// <summary>
        /// Gets the file caching mode used by this module.
        /// </summary>
        /// <value>
        /// The file caching mode.
        /// </value>
        /// <seealso cref="FileCachingMode"/>
        public FileCachingMode FileCachingMode { get; }

        /// <summary>
        /// <para>Gets or sets the default document. Defaults to <c>"index.html"</c>.</para>
        /// <para>Example: <c>"root.xml"</c>.</para>
        /// </summary>
        /// <value>
        /// The default document.
        /// </value>
        public string DefaultDocument { get; }

        /// <summary>
        /// <para>Gets or sets the default extension. Defaults to <see langword="null"/>.</para>
        /// <para>Example: <c>".html"</c>.</para>
        /// </summary>
        /// <value>
        /// The default extension.
        /// </value>
        public string DefaultExtension { get; }

        /// <summary>
        /// Gets a value indicating whether [use directory browser].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use directory browser]; otherwise, <c>false</c>.
        /// </value>
        public bool UseDirectoryBrowser { get; }

        /// <summary>
        /// Gets or sets the maximum size of the ram cache file. The default value is 250kb.
        /// </summary>
        /// <value>
        /// The maximum size of the ram cache file.
        /// </value>
        public int MaxRamCacheFileSize { get; set; } = 250 * 1024;

        private RamCache RamCache { get; } = new RamCache();

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache() => RamCache.Clear();

        private static string ValidateDefaultDocument(string argumentName, string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Default document contains one or more invalid characters.", argumentName);

            return value;
        }

        private static string ValidateDefaultExtension(string argumentName, string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value[0] != '.')
                throw new ArgumentException("Default extension does not start with a dot.", argumentName);

            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Default extension contains one or more invalid characters.", argumentName);

            return value;
        }

        private Task<bool> HandleGet(IHttpContext context, bool sendBuffer, CancellationToken ct)
        {
            switch (MapUrlPath(context.RequestPath(), out var localPath))
            {
                case PathMappingResult.IsFile:
                    return HandleFile(context, localPath, sendBuffer, ct);
                case PathMappingResult.IsDirectory:
                    return HandleDirectory(context, localPath, ct);
                default:
                    return Task.FromResult(false);
            }
        }

        private PathMappingResult MapUrlPath(string urlPath, out string localPath)
        {
            urlPath = UrlPath.Normalize(urlPath, false);
            PathMappingResult result;
            (result, localPath) = FileCachingMode >= FileCachingMode.MappingOnly ? _pathCache.GetOrAdd(urlPath, MapUrlPathCore) : MapUrlPathCore(urlPath);
            return result;
        }

        private (PathMappingResult, string) MapUrlPathCore(string urlPath)
        {
            var localPath = MapUrlPathToLoLocalPath(urlPath);
            
            // Error 404 on failed mapping.
            var validationResult = localPath == null
                ? PathMappingResult.NotFound
                : ValidateLocalPath(ref localPath);

            return (validationResult, localPath);
        }

        private string MapUrlPathToLoLocalPath(string urlPath)
        {
            var relativeUrlPath = UrlPath.UnsafeStripPrefix(urlPath, BaseUrlPath);
            if (relativeUrlPath == null)
                return null;

            string localPath;

            // Disable CA1031 as there's little we can do if IsPathRooted or GetFullPath fails.
#pragma warning disable CA1031
            try
            {
                // Bail out early if the path is a rooted path,
                // as Path.Combine would ignore our base path.
                // See https://docs.microsoft.com/en-us/dotnet/api/system.io.path.combine
                // (particularly the Remarks section).
                //
                // Under Windows, a relative URL path may be a full filesystem path
                // (e.g. "D:\foo\bar" or "\\192.168.0.1\Shared\MyDocuments\BankAccounts.docx").
                // Under Unix-like operating systems we have no such problems, as relativeUrlPath
                // can never start with a slash; however, loading one more class from Swan
                // just to check the OS type would probably outweigh calling IsPathRooted.
                if (Path.IsPathRooted(relativeUrlPath))
                    return null;

                // Convert the relative URL path to a relative filesystem path
                // (practically a no-op under Unix-like operating systems)
                // and combine it with our base local path to obtain a full path.
                localPath = Path.Combine(FileSystemPath, relativeUrlPath.Replace('/', Path.DirectorySeparatorChar));

                // Use GetFullPath as an additional safety check
                // for relative paths that contain a rooted path
                // (e.g. "valid/path/C:\Windows\System.ini")
                localPath = Path.GetFullPath(localPath);
            }
            catch
            {
                // Both IsPathRooted and GetFullPath throw exceptions
                // if a path contains invalid characters or is otherwise invalid;
                // bail out in this case too, as the path would not exist on disk anyway.
                return null;
            }
#pragma warning restore CA1031

            // As a final precaution, check that the resulting local path
            // is inside the folder intended to be served.
            if (!localPath.StartsWith(FileSystemPath, StringComparison.Ordinal))
                return null;

            return localPath;
        }

        private PathMappingResult ValidateLocalPath(ref string localPath)
        {
            if (File.Exists(localPath))
                return PathMappingResult.IsFile;

            if (Directory.Exists(localPath))
            {
                if (DefaultDocument != null)
                {
                    localPath = Path.Combine(localPath, DefaultDocument);
                    if (File.Exists(localPath))
                        return PathMappingResult.IsFile;
                }

                if (UseDirectoryBrowser)
                    return PathMappingResult.IsDirectory;
            }

            if (DefaultExtension != null)
            {
                localPath += DefaultExtension;
                if (File.Exists(localPath))
                    return PathMappingResult.IsFile;
            }

            localPath = null;
            return PathMappingResult.NotFound;
        }

        private async Task<bool> HandleFile(
            IHttpContext context,
            string localPath,
            bool sendBuffer,
            CancellationToken ct)
        {
            Stream buffer = null;

            try
            {
                var isTagValid = false;
                var partialHeader = context.RequestHeader(HttpHeaders.Range);
                var usingPartial = partialHeader?.StartsWith("bytes=") == true;
                var fileInfo = new FileInfo(localPath);

                if (sendBuffer)
                    buffer = GetFileStream(context, fileInfo, usingPartial, out isTagValid);

                // check to see if the file was modified or e-tag is the same
                var utcFileDateString = fileInfo.LastWriteTimeUtc
                    .ToString(Strings.BrowserTimeFormat, CultureInfo.InvariantCulture);

                if (!usingPartial &&
                    (isTagValid || context.RequestHeader(HttpHeaders.IfModifiedSince).Equals(utcFileDateString)))
                {
                    SetStatusCode304(context.Response);
                    return true;
                }

                context.Response.ContentLength64 = fileInfo.Length;

                SetGeneralHeaders(context.Response, utcFileDateString, fileInfo.Extension);

                if (!sendBuffer)
                {
                    return true;
                }

                // If buffer is null something is really wrong
                if (buffer == null)
                {
                    return false;
                }

                await WriteFileAsync(
                        partialHeader,
                        context.Response,
                        buffer,
                        context.AcceptGzip(buffer.Length),
                        ct)
                    .ConfigureAwait(false);
            }
            catch (HttpListenerException)
            {
                // Connection error, nothing else to do
            }
            catch (Net.HttpListenerException)
            {
                // Connection error, nothing else to do
            }
            finally
            {
                buffer?.Dispose();
            }

            return true;
        }

        private Task<bool> HandleDirectory(IHttpContext context, string localPath, CancellationToken ct)
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
                                Size = "-",
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
                                    Size = fileInfo.Length.FormatBytes(),
                                };
                            })
                            .OrderBy(x => x.Name))
                        .Select(y => $"<a href='{y.Url}'>{WebUtility.HtmlEncode(y.Name)}</a>" +
                                     new string(' ', MaxEntryLength - y.Name.Length + 1) +
                                     y.ModificationTime.ToString(Strings.BrowserTimeFormat,
                                         CultureInfo.InvariantCulture) +
                                     new string(' ', SizeIndent - y.Size.Length) +
                                     y.Size))
                .Where(x => !string.IsNullOrWhiteSpace(x));

            var content = Responses.ResponseBaseHtml.Replace(
                "{0}",
                $"<h1>Index of {WebUtility.HtmlEncode(context.RequestPath())}</h1><hr/><pre>{string.Join("\n", entries)}</pre><hr/>");

            return context.HtmlResponseAsync(content, cancellationToken: ct);
        }

        private Stream GetFileStream(IHttpContext context, FileSystemInfo fileInfo, bool usingPartial, out bool isTagValid)
        {
            isTagValid = false;
            var localPath = fileInfo.FullName;

            if (FileCachingMode == FileCachingMode.Complete && RamCache.IsValid(localPath, fileInfo.LastWriteTime, out var currentHash))
            {
                isTagValid = context.RequestHeader(HttpHeaders.IfNoneMatch) == currentHash;

                if (isTagValid)
                {
                    $"RAM Cache: {localPath}".Debug(nameof(StaticFilesModule));

                    context.Response.AddHeader(HttpHeaders.ETag, currentHash);
                    return new MemoryStream(RamCache[localPath].Buffer);
                }
            }

            $"File System: {localPath}".Debug(nameof(StaticFilesModule));

            var buffer = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (usingPartial == false)
            {
                isTagValid = UpdateFileCache(
                    context.Response,
                    buffer,
                    fileInfo.LastWriteTime,
                    context.RequestHeader(HttpHeaders.IfNoneMatch),
                    localPath);
            }

            return buffer;
        }

        private bool UpdateFileCache(
            IHttpResponse response,
            Stream buffer,
            DateTime fileDate,
            string requestHash,
            string localPath)
        {
            var currentHash = _fileHashCache.TryGetValue(localPath, out var currentTuple) &&
                              fileDate.Ticks == currentTuple.DateTicks
                ? currentTuple.HashCode
                : $"{buffer.ComputeMD5().ToUpperHex()}-{fileDate.Ticks}";

            _fileHashCache.TryAdd(localPath, (fileDate.Ticks, currentHash));

            if (!string.IsNullOrWhiteSpace(requestHash) && requestHash == currentHash)
            {
                return true;
            }

            if (FileCachingMode == FileCachingMode.Complete && buffer.Length <= MaxRamCacheFileSize)
            {
                RamCache.Add(buffer, localPath, fileDate);
            }

            response.AddHeader(HttpHeaders.ETag, currentHash);

            return false;
        }

        private void SetStatusCode304(IHttpResponse response)
        {
            SetDefaultCacheHeaders(response);

            response.ContentType = string.Empty;
            response.StatusCode = 304;
        }
    }
}
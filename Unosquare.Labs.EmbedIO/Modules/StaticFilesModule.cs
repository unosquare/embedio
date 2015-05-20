namespace Unosquare.Labs.EmbedIO.Modules
{
    using System;
    using System.Globalization;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Unosquare.Labs.EmbedIO;

    /// <summary>
    /// Represents a simple module to server static files from the file system.
    /// </summary>
    public class StaticFilesModule : WebModuleBase
    {
        /// <summary>
        /// Default document constant to "index.html"
        /// </summary>
        public const string DefaultDocumentName = "index.html";

        /// <summary>
        /// Gets or sets the maximum size of the ram cache file.
        /// </summary>
        /// <value>
        /// The maximum size of the ram cache file.
        /// </value>
        public int MaxRamCacheFileSize { get; set; }

        /// <summary>
        /// Gets or sets the default document.
        /// Defaults to "index.html"
        /// Example: "root.xml"
        /// </summary>
        /// <value>
        /// The default document.
        /// </value>
        public string DefaultDocument { get; set; }

        /// <summary>
        /// Gets or sets the default extension.
        /// Defaults to null
        /// Example: ".html"
        /// </summary>
        /// <value>
        /// The default extension.
        /// </value>
        public string DefaultExtension { get; set; }


        private Dictionary<string, string> m_MimeTypes;

        /// <summary>
        /// Gets the collection holding the MIME types.
        /// </summary>
        /// <value>
        /// The MIME types.
        /// </value>
        public Dictionary<string, string> MimeTypes
        {
            get { return m_MimeTypes; }
        }

        /// <summary>
        /// Gets the file system path from which files are retrieved.
        /// </summary>
        /// <value>
        /// The file system path.
        /// </value>
        public string FileSystemPath { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to use the RAM Cache feature
        /// RAM Cache will only cache files that are MaxRamCacheSize in bytes or less
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use ram cache]; otherwise, <c>false</c>.
        /// </value>
        public bool UseRamCache { get; set; }

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name
        {
            get { return "Static Files Module"; }
        }

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache()
        {
            this.RamCache.Clear();
        }

        /// <summary>
        /// Private collection holding the contents of the RAM Cache.
        /// </summary>
        /// <value>
        /// The ram cache.
        /// </value>
        private ConcurrentDictionary<string, RamCacheEntry> RamCache { get; set; }

        /// <summary>
        /// Represents a RAM Cache dictionary entry
        /// </summary>
        private class RamCacheEntry
        {
            public DateTime LastModified { get; set; }
            public byte[] Buffer { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <exception cref="System.ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesModule(string fileSystemPath)
            : base()
        {
            if (Directory.Exists(fileSystemPath) == false)
                throw new ArgumentException("Path '" + fileSystemPath + "' does not exist.");

            this.FileSystemPath = fileSystemPath;
#if DEBUG
            // When debugging, disable RamCache
            this.UseRamCache = false;
#else
            // Otherwise, enable it by default
            this.UseRamCache = true;
#endif
            this.RamCache = new ConcurrentDictionary<string, RamCacheEntry>(StringComparer.InvariantCultureIgnoreCase);
            this.MaxRamCacheFileSize = 250 * 1024;
            this.DefaultDocument = DefaultDocumentName;

            // Populate the default MIME types
            this.m_MimeTypes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var kvp in Constants.DefaultMimeTypes)
            {
                this.m_MimeTypes.Add(kvp.Key, kvp.Value);
            }

            this.AddHandler(ModuleMap.AnyPath, HttpVerbs.Head, (server, context) => HandleGet(context, server, false));
            this.AddHandler(ModuleMap.AnyPath, HttpVerbs.Get, (server, context) => HandleGet(context, server));
        }

        private bool HandleGet(HttpListenerContext context, WebServer server, bool sendBuffer = true)
        {
            var urlPath = context.Request.Url.LocalPath.Replace('/', Path.DirectorySeparatorChar);

            // adjust the path to see if we've got a default document
            if (urlPath.Last() == Path.DirectorySeparatorChar)
                urlPath = urlPath + DefaultDocument;

            urlPath = urlPath.TrimStart(new char[] { Path.DirectorySeparatorChar });

            var localPath = Path.Combine(FileSystemPath, urlPath);
            var eTagValid = false;
            byte[] buffer = null;
            var fileDate = DateTime.Today;

            if (string.IsNullOrWhiteSpace(DefaultExtension) == false && DefaultExtension.StartsWith(".") &&
                File.Exists(localPath) == false)
            {
                var newPath = localPath + DefaultExtension;
                if (File.Exists(newPath))
                    localPath = newPath;
            }

            if (File.Exists(localPath))
            {
                fileDate = File.GetLastWriteTime(localPath);
                var requestHash = context.RequestHeader(Constants.HeaderIfNotMatch);

                if (RamCache.ContainsKey(localPath) && RamCache[localPath].LastModified == fileDate)
                {
                    server.Log.DebugFormat("RAM Cache: {0}", localPath);
                    var currentHash = Extensions.ComputeMd5Hash(RamCache[localPath].Buffer) + '-' + fileDate.Ticks;

                    if (String.IsNullOrWhiteSpace(requestHash) || requestHash != currentHash)
                    {
                        buffer = RamCache[localPath].Buffer;
                        context.Response.AddHeader(Constants.HeaderETag, currentHash);
                    }
                    else
                    {
                        eTagValid = true;
                    }
                }
                else
                {
                    server.Log.DebugFormat("File System: {0}", localPath);
                    buffer = File.ReadAllBytes(localPath);
                    var currentHash = Extensions.ComputeMd5Hash(buffer) + '-' + fileDate.Ticks;

                    if (String.IsNullOrWhiteSpace(requestHash) || requestHash != currentHash)
                    {
                        if (UseRamCache && buffer.Length <= MaxRamCacheFileSize)
                        {
                            RamCache[localPath] = new RamCacheEntry() { LastModified = fileDate, Buffer = buffer };
                        }

                        context.Response.AddHeader(Constants.HeaderETag, currentHash);
                    }
                    else
                    {
                        eTagValid = true;
                    }
                }
            }
            else
            {
                return false;
            }

            // check to see if the file was modified or etag is the same
            var utcFileDateString = fileDate.ToUniversalTime().ToString(Constants.BrowserTimeFormat, CultureInfo.InvariantCulture);
            if (eTagValid || context.RequestHeader(Constants.HeaderIfModifiedSince).Equals(utcFileDateString))
            {
                context.Response.AddHeader(Constants.HeaderCacheControl, "private");
                context.Response.AddHeader(Constants.HeaderPragma, string.Empty);
                context.Response.AddHeader(Constants.HeaderExpires, string.Empty);
                context.Response.ContentType = string.Empty;

                context.Response.StatusCode = 304;
            }
            else
            {
                var extension = Path.GetExtension(localPath).ToLowerInvariant();
                if (MimeTypes.ContainsKey(extension))
                    context.Response.ContentType = MimeTypes[extension];

                context.Response.AddHeader(Constants.HeaderCacheControl, "private");
                context.Response.AddHeader(Constants.HeaderPragma, string.Empty);
                context.Response.AddHeader(Constants.HeaderExpires, string.Empty);
                context.Response.AddHeader(Constants.HeaderLastModified, utcFileDateString);
                context.Response.AddHeader(Constants.HeaderAcceptRanges, "bytes");

                if (sendBuffer)
                {
                    var lrange = 0;
                    var urange = buffer.Length;
                    var size = buffer.LongLength;
                    var isPartial = false;
                    var partialHeader = context.RequestHeader(Constants.HeaderRange);

                    if (String.IsNullOrWhiteSpace(partialHeader) == false && partialHeader.StartsWith("bytes="))
                    {
                        var range = partialHeader.Replace("bytes=", "").Split('-');
                        if (range.Length == 2 && int.TryParse(range[0], out lrange) &&
                            int.TryParse(range[1], out urange))
                        {
                            urange = urange > buffer.Length ? buffer.Length : urange;
                            isPartial = true;
                        }

                        if ((range.Length == 2 && int.TryParse(range[0], out lrange) &&
                             String.IsNullOrWhiteSpace(range[1])) ||
                            (range.Length == 1 && int.TryParse(range[0], out lrange)))
                        {
                            urange = buffer.Length - 1;
                            isPartial = true;
                        }
                    }

                    if (isPartial)
                    {
                        size = (urange - lrange) + 1;

                        context.Response.AddHeader(Constants.HeaderContentRanges,
                            String.Format("bytes {0}-{1}/{2}", lrange, urange - 1, buffer.Length));

                        context.Response.StatusCode = 206;
                    }
                    else
                    {
                        // Perform compression if available
                        if (context.RequestHeader(Constants.HeaderAcceptEncoding).Contains("gzip"))
                        {
                            buffer = buffer.Compress();
                            context.Response.AddHeader(Constants.HeaderContentEncoding, "gzip");
                            size = buffer.LongLength;
                            lrange = 0;
                            urange = buffer.Length;
                        }
                    }

                    context.Response.ContentLength64 = size;
                    context.Response.OutputStream.Write(buffer, lrange, (int)size);
                }
                else
                {
                    context.Response.ContentLength64 = buffer.LongLength;
                }
            }

            return true;
        }


    }
}
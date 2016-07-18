﻿namespace Unosquare.Labs.EmbedIO.Modules {
    using EmbedIO;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;

    /// <summary>
    /// Represents a simple module to server static files from the file system.
    /// </summary>
    public class StaticFilesModule : WebModuleBase {
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


        private readonly Dictionary<string, string> m_MimeTypes;

        /// <summary>
        /// Gets the collection holding the MIME types.
        /// </summary>
        /// <value>
        /// The MIME types.
        /// </value>
        public Dictionary<string, string> MimeTypes {
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
        /// The default headers
        /// </summary>
        public Dictionary<string, string> DefaultHeaders = new Dictionary<string, string> ();

        /// <summary>
        /// Gets the name of this module.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "Static Files Module";

        /// <summary>
        /// Clears the RAM cache.
        /// </summary>
        public void ClearRamCache () {
            this.RamCache.Clear ();
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
        private class RamCacheEntry {
            public DateTime LastModified { get; set; }
            public byte[] Buffer { get; set; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFilesModule" /> class.
        /// </summary>
        /// <param name="fileSystemPath">The file system path.</param>
        /// <param name="headers">The headers to set in every request.</param>
        /// <exception cref="System.ArgumentException">Path ' + fileSystemPath + ' does not exist.</exception>
        public StaticFilesModule (string fileSystemPath, Dictionary<string, string> headers = null)
            : base () {
            if (Directory.Exists (fileSystemPath) == false)
                throw new ArgumentException ("Path '" + fileSystemPath + "' does not exist.");

            this.FileSystemPath = fileSystemPath;
#if DEBUG
            // When debugging, disable RamCache
            this.UseRamCache = false;
#else
            // Otherwise, enable it by default
            this.UseRamCache = true;
#endif
            this.RamCache = new ConcurrentDictionary<string, RamCacheEntry> (StringComparer.InvariantCultureIgnoreCase);
            this.MaxRamCacheFileSize = 250 * 1024;
            this.DefaultDocument = DefaultDocumentName;

            // Populate the default MIME types
            this.m_MimeTypes = new Dictionary<string, string> (StringComparer.InvariantCultureIgnoreCase);
            foreach (var kvp in Constants.DefaultMimeTypes) {
                this.m_MimeTypes.Add (kvp.Key, kvp.Value);
            }

            if (headers != null) {
                foreach (var header in headers) {
                    this.DefaultHeaders.Add (header.Key, header.Value);
                }
            }

            this.AddHandler (ModuleMap.AnyPath, HttpVerbs.Head, (server, context) => HandleGet (context, server, false));
            this.AddHandler (ModuleMap.AnyPath, HttpVerbs.Get, (server, context) => HandleGet (context, server));
        }
        /// <summary>
        ///  find matching UrlRoot for it and remove it from requestPath
        /// </summary>
        /// <param name="requestPath"></param>
        /// <param name="urlRoots"></param>
        /// <returns></returns>
        private string RemoveServerRootFromRequestUrl (string requestPath, IEnumerable<string> urlRoots) {
            var url = requestPath.ToLowerInvariant ();
            if (requestPath == string.Empty || requestPath[0] != '/') url = "/" + url;
            var urlRootsWithoutLastSlash = urlRoots.Select (x => x.Substring (0, x.Length - 1)); //all url roots end with slash; request might or might not end with slash
            var urlRootMatch = urlRootsWithoutLastSlash.Where (x => url.StartsWith (x)).OrderByDescending (x => x.Length).FirstOrDefault ();
            //OrderByDescending : the longest match rule applies when routing requests in HTTP.sys, I'm consistent with it; This matter only if there are multiple prefixes in webserver and they have common start, like: /AAA/ and /AAA/BBB/ or / and /AAA
            if (urlRootMatch == null) {
                //should never happen, such requests would never reach HttpListener, unless there is bug this code
                throw new ArgumentException ("Could not find UrlRoot for request url " + url + ". UrlRoots are :" + String.Join (", ", urlRoots));
            } else {
                var shortened = url.Substring (urlRootMatch.Length);
                if (shortened == string.Empty || shortened[0] != '/') {
                    shortened = "/" + shortened;
                }
                return shortened;
            }
        }

        private bool HandleGet (HttpListenerContext context, WebServer server, bool sendBuffer = true) {
            var urlPath = RemoveServerRootFromRequestUrl (context.RequestPath (), server.UrlRoots);
            urlPath = urlPath.Replace ('/', Path.DirectorySeparatorChar);

            // adjust the path to see if we've got a default document
            if (urlPath.Last () == Path.DirectorySeparatorChar)
                urlPath = urlPath + DefaultDocument;

            urlPath = urlPath.TrimStart (new char[] { Path.DirectorySeparatorChar });

            var localPath = Path.Combine (FileSystemPath, urlPath);
            var eTagValid = false;
            byte[] buffer = null;
            var fileDate = DateTime.Today;
            var partialHeader = context.RequestHeader (Constants.HeaderRange);
            var usingPartial = string.IsNullOrWhiteSpace (partialHeader) == false && partialHeader.StartsWith ("bytes=");

            if (string.IsNullOrWhiteSpace (DefaultExtension) == false && DefaultExtension.StartsWith (".") &&
                File.Exists (localPath) == false) {
                var newPath = localPath + DefaultExtension;

                if (File.Exists (newPath)) {
                    localPath = newPath;
                }
            }

            if (File.Exists (localPath) == false) {
                if (Directory.Exists (localPath) && File.Exists (Path.Combine (localPath, DefaultDocument))) {
                    localPath = Path.Combine (localPath, DefaultDocument);
                } else {
                    return false;
                }
            }

            if (usingPartial == false) {
                fileDate = File.GetLastWriteTime (localPath);
                var requestHash = context.RequestHeader (Constants.HeaderIfNotMatch);

                if (RamCache.ContainsKey (localPath) && RamCache[localPath].LastModified == fileDate) {
                    server.Log.DebugFormat ("RAM Cache: {0}", localPath);
                    var currentHash = Extensions.ComputeMd5Hash (RamCache[localPath].Buffer) + '-' + fileDate.Ticks;

                    if (string.IsNullOrWhiteSpace (requestHash) || requestHash != currentHash) {
                        buffer = RamCache[localPath].Buffer;
                        context.Response.AddHeader (Constants.HeaderETag, currentHash);
                    } else {
                        eTagValid = true;
                    }
                } else {
                    server.Log.DebugFormat ("File System: {0}", localPath);

                    if (sendBuffer) {
                        buffer = File.ReadAllBytes (localPath);

                        var currentHash = Extensions.ComputeMd5Hash (buffer) + '-' + fileDate.Ticks;

                        if (string.IsNullOrWhiteSpace (requestHash) || requestHash != currentHash) {
                            if (UseRamCache && buffer.Length <= MaxRamCacheFileSize) {
                                RamCache[localPath] = new RamCacheEntry () { LastModified = fileDate, Buffer = buffer };
                            }

                            context.Response.AddHeader (Constants.HeaderETag, currentHash);
                        } else {
                            eTagValid = true;
                        }
                    }
                }
            }

            // check to see if the file was modified or etag is the same
            var utcFileDateString = fileDate.ToUniversalTime ()
                .ToString (Constants.BrowserTimeFormat, Constants.StandardCultureInfo);
            if (usingPartial == false &&
                (eTagValid || context.RequestHeader (Constants.HeaderIfModifiedSince).Equals (utcFileDateString))) {
                context.Response.AddHeader (Constants.HeaderCacheControl,
                    DefaultHeaders.ContainsKey (Constants.HeaderCacheControl)
                        ? DefaultHeaders[Constants.HeaderCacheControl]
                        : "private");

                context.Response.AddHeader (Constants.HeaderPragma,
                    DefaultHeaders.ContainsKey (Constants.HeaderPragma)
                        ? DefaultHeaders[Constants.HeaderPragma]
                        : string.Empty);

                context.Response.AddHeader (Constants.HeaderExpires,
                    DefaultHeaders.ContainsKey (Constants.HeaderExpires)
                        ? DefaultHeaders[Constants.HeaderExpires]
                        : string.Empty);

                context.Response.ContentType = string.Empty;

                context.Response.StatusCode = 304;
            } else {
                var fileExtension = Path.GetExtension (localPath).ToLowerInvariant ();
                if (MimeTypes.ContainsKey (fileExtension))
                    context.Response.ContentType = MimeTypes[fileExtension];

                context.Response.AddHeader (Constants.HeaderCacheControl,
                    DefaultHeaders.ContainsKey (Constants.HeaderCacheControl)
                        ? DefaultHeaders[Constants.HeaderCacheControl]
                        : "private");

                context.Response.AddHeader (Constants.HeaderPragma,
                    DefaultHeaders.ContainsKey (Constants.HeaderPragma)
                        ? DefaultHeaders[Constants.HeaderPragma]
                        : string.Empty);

                context.Response.AddHeader (Constants.HeaderExpires,
                    DefaultHeaders.ContainsKey (Constants.HeaderExpires)
                        ? DefaultHeaders[Constants.HeaderExpires]
                        : string.Empty);

                context.Response.AddHeader (Constants.HeaderLastModified, utcFileDateString);
                context.Response.AddHeader (Constants.HeaderAcceptRanges, "bytes");

                if (sendBuffer) {
                    var lowerByteIndex = 0;
                    var upperByteIndex = 0;
                    var byteLength = (long)0;
                    var isPartial = false;
                    var fileSize = new FileInfo (localPath).Length;

                    if (usingPartial) {
                        var range = partialHeader.Replace ("bytes=", "").Split ('-');
                        if (range.Length == 2 && int.TryParse (range[0], out lowerByteIndex) &&
                            int.TryParse (range[1], out upperByteIndex)) {
                            isPartial = true;
                        }

                        if ((range.Length == 2 && int.TryParse (range[0], out lowerByteIndex) &&
                             string.IsNullOrWhiteSpace (range[1])) ||
                            (range.Length == 1 && int.TryParse (range[0], out lowerByteIndex))) {
                            upperByteIndex = (int)fileSize - 1;
                            isPartial = true;
                        }

                        if (range.Length == 2 && string.IsNullOrWhiteSpace (range[0]) &&
                            int.TryParse (range[1], out upperByteIndex)) {
                            lowerByteIndex = (int)fileSize - upperByteIndex;
                            upperByteIndex = (int)fileSize - 1;
                            isPartial = true;
                        }
                    }

                    if (isPartial) {
                        if (upperByteIndex > fileSize) {
                            context.Response.StatusCode = 416;
                            context.Response.AddHeader (Constants.HeaderContentRanges,
                                string.Format ("bytes */{0}", fileSize));
                            return true;
                        }

                        byteLength = (upperByteIndex - lowerByteIndex) + 1;

                        context.Response.AddHeader (Constants.HeaderContentRanges,
                            string.Format ("bytes {0}-{1}/{2}", lowerByteIndex, upperByteIndex, fileSize));

                        context.Response.StatusCode = 206;

                        server.Log.DebugFormat ("Opening stream {0} bytes {1}-{2} size {3}", localPath, lowerByteIndex, upperByteIndex,
                            byteLength);

                        buffer = new byte[byteLength];

                        // Open FileStream with FileShare
                        using (var fs = new FileStream (localPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                            if (lowerByteIndex + byteLength > fs.Length) byteLength = fs.Length - lowerByteIndex;
                            fs.Seek (lowerByteIndex, SeekOrigin.Begin);
                            fs.Read (buffer, 0, (int)byteLength);
                            fs.Close ();
                        }

                        // Reset lower range
                        lowerByteIndex = 0;
                    } else {
                        byteLength = buffer.LongLength;

                        // Perform compression if available
                        if (context.RequestHeader (Constants.HeaderAcceptEncoding).Contains ("gzip")) {
                            buffer = buffer.Compress ();
                            context.Response.AddHeader (Constants.HeaderContentEncoding, "gzip");
                            byteLength = buffer.LongLength;
                            lowerByteIndex = 0;
                        }
                    }

                    context.Response.ContentLength64 = byteLength;

                    try {
                        context.Response.OutputStream.Write (buffer, lowerByteIndex, (int)byteLength);
                    } catch (HttpListenerException) {
                        // Connection error, nothing else to do
                    }
                } else {
                    context.Response.ContentLength64 = buffer == null
                        ? new FileInfo (localPath).Length
                        : buffer.LongLength;
                }
            }

            return true;
        }
    }
}

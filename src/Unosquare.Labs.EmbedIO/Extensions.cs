namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using Core;
    using Swan;
    using Swan.Formatters;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods to help your coding.
    /// </summary>
    public static partial class Extensions
    {
        private static readonly byte[] LastByte = { 0x00 };

        private static readonly Regex RouteOptionalParamRegex = new Regex(@"\{[^\/]*\?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #region Session Management Methods

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>A session object for the given server context.</returns>
        public static SessionInfo GetSession(this IHttpContext context)
            => context.WebServer.SessionModule?.GetSession(context);

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void DeleteSession(this IHttpContext context)
        {
            context.WebServer.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the given session object.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="session">The session info.</param>
        public static void DeleteSession(this IHttpContext context, SessionInfo session)
        {
            context.WebServer.SessionModule?.DeleteSession(session);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns>A session info for the given websocket context.</returns>
        public static SessionInfo GetSession(this IWebSocketContext context, IWebServer server) => server.SessionModule?.GetSession(context);

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns>A session info for the given websocket context.</returns>
        public static SessionInfo GetSession(this IWebServer server, IWebSocketContext context) => server.SessionModule?.GetSession(context);

        #endregion

        #region HTTP Request Helpers

        /// <summary>
        /// Gets the request path for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Path for the specified context.</returns>
        public static string RequestPath(this IHttpContext context)
            => context.Request.Url.LocalPath.ToLowerInvariant();

        /// <summary>
        /// Gets the request path for the specified context using a wildcard paths to 
        /// match.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="wildcardPaths">The wildcard paths.</param>
        /// <returns>Path for the specified context.</returns>
        public static string RequestWilcardPath(this IHttpContext context, IEnumerable<string> wildcardPaths)
        {
            var path = context.Request.Url.LocalPath.ToLowerInvariant();

            var wildcardMatch = wildcardPaths.FirstOrDefault(p => // wildcard at the end
                path.StartsWith(p.Substring(0, p.Length - ModuleMap.AnyPath.Length))

                // wildcard in the middle so check both start/end
                || (path.StartsWith(p.Substring(0, p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal)))
                    && path.EndsWith(p.Substring(p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal) + 1))));

            return string.IsNullOrWhiteSpace(wildcardMatch) ? path : wildcardMatch;
        }

        /// <summary>
        /// Gets the request path for the specified context case sensitive.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Path for the specified context.</returns>
        public static string RequestPathCaseSensitive(this IHttpContext context)
            => context.Request.Url.LocalPath;

        /// <summary>
        /// Retrieves the Request HTTP Verb (also called Method) of this context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>HTTP verb result of the conversion of this context.</returns>
        public static HttpVerbs RequestVerb(this IHttpContext context)
        {
            Enum.TryParse(context.Request.HttpMethod.Trim(), true, out HttpVerbs verb);
            return verb;
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// If the value does not exist it returns null.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns>A string that represents the value for the specified query string key.</returns>
        public static string QueryString(this IHttpContext context, string key)
            => context.InQueryString(key) ? context.Request.QueryString[key] : null;

        /// <summary>
        /// Determines if a key exists within the Request's query string.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if a key exists within the Request's query string; otherwise, <c>false</c>.</returns>
        public static bool InQueryString(this IHttpContext context, string key)
            => context.Request.QueryString.AllKeys.Contains(key);

        /// <summary>
        /// Retrieves the specified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns>Specified request the header when is <c>true</c>; otherwise, empty string.</returns>
        public static string RequestHeader(this IHttpContext context, string headerName)
            => context.Request.Headers[headerName] ?? string.Empty;

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns><c>true</c> if request headers is not a null; otherwise, false.</returns>
        public static bool HasRequestHeader(this IHttpContext context, string headerName)
            => context.Request.Headers[headerName] != null;

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as 
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// The rest of the stream as a string, from the current position to the end.
        /// If the current position is at the end of the stream, returns an empty string.
        /// </returns>
        [Obsolete("Please use the async method.")]
        public static string RequestBody(this IHttpContext context)
        {
            if (!context.Request.HasEntityBody)
                return null;

            using (var body = context.Request.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, context.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as 
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A task with the rest of the stream as a string, from the current position to the end.
        /// If the current position is at the end of the stream, returns an empty string.
        /// </returns>
        public static async Task<string> RequestBodyAsync(this IHttpContext context)
        {
            if (!context.Request.HasEntityBody)
                return null;

            using (var body = context.Request.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, context.Request.ContentEncoding))
                {
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Requests the wildcard URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        public static string[] RequestWildcardUrlParams(this IHttpContext context, string basePath)
            => RequestWildcardUrlParams(context.RequestPath(), basePath);

        /// <summary>
        /// Requests the wildcard URL parameters.
        /// </summary>
        /// <param name="requestPath">The request path.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        public static string[] RequestWildcardUrlParams(this string requestPath, string basePath)
        {
            var match = RegexCache.MatchWildcardStrategy(basePath, requestPath);

            return match.Success
                ? match.Groups[1].Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                : null;
        }

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="urlPattern">The url pattern. </param>
        /// <returns>The params from the request.</returns>
        public static Dictionary<string, object> RequestRegexUrlParams(this IWebSocketContext context, string urlPattern)
          => RequestRegexUrlParams(context.RequestUri.LocalPath, urlPattern);

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        public static Dictionary<string, object> RequestRegexUrlParams(this IHttpContext context,
            string basePath)
            => RequestRegexUrlParams(context.RequestPath(), basePath);

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="requestPath">The request path.</param>
        /// <param name="basePath">The base path.</param>
        /// <param name="validateFunc">The validate function.</param>
        /// <returns>
        /// The params from the request.
        /// </returns>
        public static Dictionary<string, object> RequestRegexUrlParams(
            this string requestPath,
            string basePath,
            Func<bool> validateFunc = null)
        {
            if (validateFunc == null) validateFunc = () => false;
            if (requestPath == basePath && !validateFunc()) return new Dictionary<string, object>();

            var i = 1; // match group index
            var match = RegexCache.MatchRegexStrategy(basePath, requestPath);
            var pathParts = basePath.Split('/');

            if (match.Success && !validateFunc())
            {
                return pathParts
                    .Where(x => x.StartsWith("{"))
                    .ToDictionary(CleanParamId, x => (object)match.Groups[i++].Value);
            }

            var optionalPath = RouteOptionalParamRegex.Replace(basePath, string.Empty);
            var tempPath = requestPath;

            if (optionalPath.Last() == '/' && requestPath.Last() != '/')
            {
                tempPath += "/";
            }

            var subMatch = RegexCache.MatchRegexStrategy(optionalPath, tempPath);

            if (!subMatch.Success || validateFunc()) return null;

            var valuesPaths = optionalPath.Split('/')
                .Where(x => x.StartsWith("{"))
                .ToDictionary(CleanParamId, x => (object)subMatch.Groups[i++].Value);

            var nullPaths = pathParts
                .Where(x => x.StartsWith("{"))
                .Select(CleanParamId);

            foreach (var nullKey in nullPaths)
            {
                if (!valuesPaths.ContainsKey(nullKey))
                    valuesPaths.Add(nullKey, null);
            }

            return valuesPaths;
        }

        /// <summary>
        /// Parses the JSON as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T">The type of specified object type.</typeparam>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Parses the JSON as a given type from the request body.
        /// </returns>
        [Obsolete("Please use the async method.")]
        public static T ParseJson<T>(this IHttpContext context)
            where T : class
        {
            var requestBody = context.RequestBody();
            return requestBody == null ? null : Json.Deserialize<T>(requestBody);
        }

        /// <summary>
        /// Parses the JSON as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T">The type of specified object type.</typeparam>
        /// <param name="context">The context.</param>
        /// <returns>
        /// A task with the JSON as a given type from the request body.
        /// </returns>
        public static async Task<T> ParseJsonAsync<T>(this IHttpContext context)
            where T : class
        {
            var requestBody = await context.RequestBodyAsync().ConfigureAwait(false);
            return requestBody == null ? null : Json.Deserialize<T>(requestBody);
        }

        /// <summary>
        /// Check if the Http Request can be gzipped (ignore audio and video content type).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="length">The length.</param>
        /// <returns><c>true</c> if a request can be gzipped; otherwise, <c>false</c>.</returns>
        public static bool AcceptGzip(this IHttpContext context, long length) =>
            context.RequestHeader(Headers.AcceptEncoding).Contains(Headers.CompressionGzip) &&
            length < Modules.FileModuleBase.MaxGzipInputLength &&
            context.Response.ContentType?.StartsWith("audio") != true &&
            context.Response.ContentType?.StartsWith("video") != true;

        #endregion

        #region Data Parsing Methods

        /// <summary>
        /// Returns a dictionary of KVPs from Request data.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns>A collection that represents KVPs from request data.</returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this string requestBody)
            => FormDataParser.ParseAsDictionary(requestBody);

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context">The context to request body as string.</param>
        /// <returns>A collection that represents KVPs from request data.</returns>
        [Obsolete("Please use the async method.")]
        public static Dictionary<string, object> RequestFormDataDictionary(this IHttpContext context)
            => RequestFormDataDictionary(context.RequestBody());

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context">The context to request body as string.</param>
        /// <returns>A task with a collection that represents KVPs from request data.</returns>
        public static async Task<Dictionary<string, object>> RequestFormDataDictionaryAsync(this IHttpContext context)
            => RequestFormDataDictionary(await context.RequestBodyAsync().ConfigureAwait(false));

        #endregion

        #region Hashing and Compression Methods

        /// <summary>
        /// Compresses the specified buffer stream using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="method">The method.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task representing the block of bytes of compressed stream.
        /// </returns>
        public static async Task<MemoryStream> CompressAsync(
            this Stream buffer,
            CompressionMethod method = CompressionMethod.Gzip,
            CompressionMode mode = CompressionMode.Compress,
            CancellationToken cancellationToken = default)
        {
            buffer.Position = 0;
            var targetStream = new MemoryStream();

            switch (method)
            {
                case CompressionMethod.Deflate:
                    if (mode == CompressionMode.Compress)
                    {
                        using (var compressor = new DeflateStream(targetStream, CompressionMode.Compress, true))
                        {
                            await buffer.CopyToAsync(compressor, 1024, cancellationToken).ConfigureAwait(false);
                            await buffer.CopyToAsync(compressor).ConfigureAwait(false);

                            // WebSocket use this
                            targetStream.Write(LastByte, 0, 1);
                            targetStream.Position = 0;
                        }
                    }
                    else
                    {
                        using (var compressor = new DeflateStream(buffer, CompressionMode.Decompress))
                        {
                            await compressor.CopyToAsync(targetStream).ConfigureAwait(false);
                        }
                    }

                    break;
                case CompressionMethod.Gzip:
                    if (mode == CompressionMode.Compress)
                    {
                        using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
                        {
                            await buffer.CopyToAsync(compressor).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        using (var compressor = new GZipStream(buffer, CompressionMode.Decompress))
                        {
                            await compressor.CopyToAsync(targetStream).ConfigureAwait(false);
                        }
                    }

                    break;
                case CompressionMethod.None:
                    await buffer.CopyToAsync(targetStream).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }

            return targetStream;
        }

        #endregion

        internal static string CleanParamId(string val) => val.ReplaceAll(string.Empty, '{', '}', '?');

        internal static Uri ToUri(this string uriString)
        {
            Uri.TryCreate(
                uriString, uriString.MaybeUri() ? UriKind.Absolute : UriKind.Relative, out var ret);

            return ret;
        }

        internal static bool MaybeUri(this string value)
        {
            var idx = value?.IndexOf(':');

            if (!idx.HasValue || idx == -1)
                return false;

            return idx < 10 && value.Substring(0, idx.Value).IsPredefinedScheme();
        }

        internal static bool IsPredefinedScheme(this string value) => value != null &&
                                                                      (value == "http" || value == "https" || value == "ws" || value == "wss");
    }
}
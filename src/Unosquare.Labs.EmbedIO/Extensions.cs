namespace Unosquare.Labs.EmbedIO
{
    using Constants;
    using Core;
    using Swan;
    using Swan.Formatters;
    using System.Net;
    using System.Text;
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
        [Obsolete("Wilcard routing will be dropped in future versions")]
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
        [Obsolete("RequestVerb() will be replaced by Request.HttpVerb in future versions")]
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
        /// A task with the rest of the stream as a string, from the current position to the end.
        /// If the current position is at the end of the stream, returns an empty string.
        /// </returns>
        public static Task<string> RequestBodyAsync(this IHttpContext context) =>
            context.Request.RequestBodyAsync();

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>
        /// A task with the rest of the stream as a string, from the current position to the end.
        /// If the current position is at the end of the stream, returns an empty string.
        /// </returns>
        public static async Task<string> RequestBodyAsync(this IHttpRequest request)
        {
            if (!request.HasEntityBody)
                return null;

            using (var body = request.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
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
        [Obsolete("Wilcard routing will be dropped in future versions")]
        public static string[] RequestWildcardUrlParams(this IHttpContext context, string basePath)
            => RequestWildcardUrlParams(context.RequestPath(), basePath);

        /// <summary>
        /// Requests the wildcard URL parameters.
        /// </summary>
        /// <param name="requestPath">The request path.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        [Obsolete("Wilcard routing will be dropped in future versions")]
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
        [Obsolete("RequestRegexUrlParams() will be replaced for a new Routing class")]
        public static Dictionary<string, object> RequestRegexUrlParams(this IWebSocketContext context, string urlPattern)
          => RequestRegexUrlParams(context.RequestUri.LocalPath, urlPattern);

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        [Obsolete("RequestRegexUrlParams() will be replaced for a new Routing class")]
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
        [Obsolete("RequestRegexUrlParams() will be replaced for a new Routing class")]
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
        /// A task with the JSON as a given type from the request body.
        /// </returns>
        public static async Task<T> ParseJsonAsync<T>(this IHttpContext context)
            where T : class
        {
            var requestBody = await context.RequestBodyAsync().ConfigureAwait(false);
            return requestBody == null ? null : Json.Deserialize<T>(requestBody);
        }

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> TransformJson<TIn, TOut>(
            this IHttpContext context,
            Func<TIn, CancellationToken, Task<TOut>> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
        {
            var requestJson = await context.ParseJsonAsync<TIn>()
                .ConfigureAwait(false);
            var responseJson = await transformFunc(requestJson, cancellationToken)
                .ConfigureAwait(false);

            return await context.JsonResponseAsync(responseJson, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Transforms the response body as JSON and write a new JSON to the request.
        /// </summary>
        /// <typeparam name="TIn">The type of the input.</typeparam>
        /// <typeparam name="TOut">The type of the output.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="transformFunc">The transform function.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// A task for writing the output stream.
        /// </returns>
        public static async Task<bool> TransformJson<TIn, TOut>(
            this IHttpContext context,
            Func<TIn, TOut> transformFunc,
            CancellationToken cancellationToken = default)
            where TIn : class
        {
            var requestJson = await context.ParseJsonAsync<TIn>()
                .ConfigureAwait(false);
            var responseJson = transformFunc(requestJson);

            return await context.JsonResponseAsync(responseJson, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Check if the Http Request can be gzipped (ignore audio and video content type).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="length">The length.</param>
        /// <returns><c>true</c> if a request can be gzipped; otherwise, <c>false</c>.</returns>
        public static bool AcceptGzip(this IHttpContext context, long length) =>
            context.RequestHeader(HttpHeaderNames.AcceptEncoding).Contains(HttpHeaders.CompressionGzip) &&
            length < Modules.FileModuleBase.MaxGzipInputLength &&
            context.Response.ContentType?.StartsWith("audio") != true &&
            context.Response.ContentType?.StartsWith("video") != true;

        /// <summary>
        /// Prepares a standard response without a body for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        public static void StandardResponseWithoutBody(this IHttpContext @this, int statusCode)
            => @this.Response.StandardResponseWithoutBody(statusCode);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        public static Task StandardHtmlResponseAsync(this IHttpContext @this, int statusCode, CancellationToken cancellationToken)
            => StandardHtmlResponseAsync(@this, statusCode, null, cancellationToken);

        /// <summary>
        /// Asynchronously sends a standard HTML response for the specified status code.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="appendAdditionalHtml">A callback function that may append additional HTML code
        /// to the response. If not <see langword="null"/>, the callback is called immediately before
        /// closing the HTML <c>body</c> tag.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the operation.</param>
        /// <returns>A <see cref="Task"/> representing the ongoing operation.</returns>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">There is no standard status description for <paramref name="statusCode"/>.</exception>
        public static Task StandardHtmlResponseAsync(
            this IHttpContext @this,
            int statusCode,
            Func<StringBuilder, StringBuilder> appendAdditionalHtml,
            CancellationToken cancellationToken)
            => @this.Response.StandardHtmlResponseAsync(statusCode, appendAdditionalHtml, cancellationToken);

        /// <summary>
        /// Sets a redirection status code and adds a <c>Location</c> header to the response.
        /// </summary>
        /// <param name="this">The <see cref="IHttpContext"/> interface on which this method is called.</param>
        /// <param name="location">The URL to which the user agent should be redirected.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <exception cref="NullReferenceException"><paramref name="this"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="location"/> is not a valid relative or absolute URL.<see langword="null"/>.</para>
        /// <para>- or -</para>
        /// <para><paramref name="statusCode"/> is not a redirection (3xx) status code.</para>
        /// </exception>
        [Obsolete("This method will change signature to: void Redirect(this IHttpContext @this, string location, int statusCode = (int)HttpStatusCode.Found)")]
        public static void Redirect(this IHttpContext @this, string location, int statusCode)
        {
            location = ValidateUrl(nameof(location), location, @this.Request.Url);

            if (statusCode < 300 || statusCode > 399)
                throw new ArgumentException("Redirect status code is not valid.", nameof(statusCode));

            @this.Response.Headers[HttpHeaders.Location] = location;
            @this.Response.StandardResponseWithoutBody(statusCode);
        }
        
        /// <summary>
        /// Sets a response static code of 302 and adds a Location header to the response
        /// in order to direct the client to a different URL.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        /// <returns><b>true</b> if the headers were set, otherwise <b>false</b>.</returns>
        [Obsolete("This method will change signature to: void Redirect(this IHttpContext @this, string location, int statusCode = (int)HttpStatusCode.Found)")]
        public static bool Redirect(this IHttpContext context, string location, bool useAbsoluteUrl = true)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetComponents(UriComponents.Scheme | UriComponents.StrongAuthority,
                    UriFormat.Unescaped);
                location = hostPath + location;
            }

            context.Redirect(location, (int) HttpStatusCode.Found);

            return true;
        }

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

        internal static T NotNull<T>(string argumentName, T value)
            where T : class
            => value ?? throw new ArgumentNullException(argumentName);

        internal static string ValidateUrl(string argumentName, string value, Uri baseUri, bool enforceHttp = false)
        {
            if (!NotNull(nameof(baseUri), baseUri).IsAbsoluteUri)
                throw new ArgumentException("Base URI is not an absolute URI.", nameof(baseUri));

            Uri uri;
            try
            {
                uri = new Uri(baseUri, new Uri(NotNull(argumentName, value), UriKind.RelativeOrAbsolute));
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException("URL is not valid.", argumentName, e);
            }

            if (enforceHttp && uri.IsAbsoluteUri && uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("URL scheme is neither HTTP nor HTTPS.", argumentName);

            return uri.ToString();
        }
    }
}
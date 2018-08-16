namespace Unosquare.Labs.EmbedIO
{
    using System.Text.RegularExpressions;
    using Constants;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using Swan;
    using Swan.Formatters;
    using System.Threading;
    using System.Threading.Tasks;
#if NET47
    using System.Net;
    using System.Net.WebSockets;

#else
    using Net;
#endif

    /// <summary>
    /// Extension methods to help your coding!
    /// </summary>
    public static partial class Extensions
    {
        #region Constants

        private const string RegexRouteReplace = "([^//]*)";
        private const string WildcardRouteReplace = "(.*)";

        private static readonly byte[] LastByte = {0x00};

        private static readonly Regex RouteOptionalParamRegex = new Regex(@"\{[^\/]*\?\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RouteParamRegex = new Regex(@"\{[^\/]*\}",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion

        #region Session Management Methods

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns>A session object for the given server context</returns>
        public static SessionInfo GetSession(this HttpListenerContext context, WebServer server)
        {
            return server.GetSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        public static void DeleteSession(this HttpListenerContext context, WebServer server)
        {
            server.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        public static void DeleteSession(this WebServer server, HttpListenerContext context)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the given session object.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="session">The session info.</param>
        public static void DeleteSession(this WebServer server, SessionInfo session)
        {
            server.SessionModule?.DeleteSession(session);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns>A session info for the given server context</returns>
        public static SessionInfo GetSession(this WebServer server, HttpListenerContext context)
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns>A session info for the given websocket context</returns>
#if NET47
        public static SessionInfo GetSession(this System.Net.WebSockets.WebSocketContext context, WebServer server)
#else
        public static SessionInfo GetSession(this WebSocketContext context, WebServer server)
#endif
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns>A session info for the given websocket context</returns>
#if NET47
        public static SessionInfo GetSession(this WebServer server, System.Net.WebSockets.WebSocketContext context)
#else
        public static SessionInfo GetSession(this WebServer server, WebSocketContext context)
#endif
        {
            return server.SessionModule?.GetSession(context);
        }

        #endregion

        #region HTTP Request Helpers

        /// <summary>
        /// Gets the request path for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Path for the specified context.</returns>
        public static string RequestPath(this HttpListenerContext context)
            => context.Request.Url.LocalPath.ToLowerInvariant();

        /// <summary>
        /// Gets the request path for the specified context using a wildcard paths to 
        /// match.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="wildcardPaths">The wildcard paths.</param>
        /// <returns>Path for the specified context.</returns>
        public static string RequestWilcardPath(this HttpListenerContext context, IEnumerable<string> wildcardPaths)
        {
            var path = context.Request.Url.LocalPath.ToLowerInvariant();

            var wildcardMatch = wildcardPaths.FirstOrDefault(p => // wildcard at the end
                path.StartsWith(p.Substring(0, p.Length - ModuleMap.AnyPath.Length))

                // wildcard in the middle so check both start/end
                || (path.StartsWith(p.Substring(0, p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal)))
                    && path.EndsWith(p.Substring(p.IndexOf(ModuleMap.AnyPath, StringComparison.Ordinal) + 1))));

            if (string.IsNullOrWhiteSpace(wildcardMatch) == false)
                path = wildcardMatch;

            return path;
        }

        /// <summary>
        /// Gets the request path for the specified context case sensitive.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Path for the specified context.</returns>
        public static string RequestPathCaseSensitive(this HttpListenerContext context)
            => context.Request.Url.LocalPath;

        /// <summary>
        /// Retrieves the Request HTTP Verb (also called Method) of this context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>HTTP verb result of the conversion of this context</returns>
        public static HttpVerbs RequestVerb(this HttpListenerContext context)
        {
            Enum.TryParse(context.Request.HttpMethod.ToLowerInvariant().Trim(), true, out HttpVerbs verb);
            return verb;
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// If the value does not exist it returns null.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns>A string that represents the value for the specified query string key</returns>
        public static string QueryString(this HttpListenerContext context, string key)
            => context.InQueryString(key) ? context.Request.QueryString[key] : null;

        /// <summary>
        /// Determines if a key exists within the Request's query string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns>True if a key exists within the Request's query string; otherwise, false</returns>
        public static bool InQueryString(this HttpListenerContext context, string key)
            => context.Request.QueryString.AllKeys.Contains(key);

        /// <summary>
        /// Retrieves the specified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns>Specified request the header when is true; otherwise, empty string </returns>
        public static string RequestHeader(this HttpListenerContext context, string headerName)
            => context.Request.Headers[headerName] ?? string.Empty;

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns>True if request headers is not a null; otherwise, false</returns>
        public static bool HasRequestHeader(this HttpListenerContext context, string headerName)
            => context.Request.Headers[headerName] != null;

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as 
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// The rest of the stream as a string, from the current position to the end.
        /// If the current position is at the end of the stream, returns an empty string
        /// </returns>
        public static string RequestBody(this HttpListenerContext context)
        {
            if (context.Request.HasEntityBody == false)
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
        /// Requests the wildcard URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        public static string[] RequestWildcardUrlParams(this HttpListenerContext context, string basePath)
            => RequestWildcardUrlParams(context.RequestPath(), basePath);

        /// <summary>
        /// Requests the wildcard URL parameters.
        /// </summary>
        /// <param name="requestPath">The request path.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        public static string[] RequestWildcardUrlParams(string requestPath, string basePath)
        {
            var match = new Regex(basePath.Replace("*", WildcardRouteReplace)).Match(requestPath);

            return match.Success
                ? match.Groups[1].Value.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries)
                : null;
        }

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="urlPattern">The url pattern </param>
        /// <returns>The params from the request.</returns>
        public static Dictionary<string,object> RequestRegexUrlParams(this WebSocketContext context, string urlPattern)
          => RequestRegexUrlParams(context.RequestUri.LocalPath, urlPattern);

        /// <summary>
        /// Requests the regex URL parameters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="basePath">The base path.</param>
        /// <returns>The params from the request.</returns>
        public static Dictionary<string, object> RequestRegexUrlParams(this HttpListenerContext context,
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

            var regex = new Regex(String.Concat("^",RouteParamRegex.Replace(basePath, RegexRouteReplace),"$"), RegexOptions.IgnoreCase);
            var match = regex.Match(requestPath);

            var pathParts = basePath.Split('/');

            if (!match.Success || validateFunc())
            {
                var optionalPath = RouteOptionalParamRegex.Replace(basePath, string.Empty);
                var tempPath = requestPath;

                if (optionalPath.Last() == '/' && requestPath.Last() != '/')
                {
                    tempPath += "/";
                }

                if (optionalPath == tempPath)
                {
                    return pathParts
                        .Where(x => x.StartsWith("{"))
                        .ToDictionary(x => x.CleanParamId(), x => (object) null);
                }
            }
            else
            {
                var i = 1; // match group index

                return pathParts
                    .Where(x => x.StartsWith("{"))
                    .ToDictionary(x => x.CleanParamId(), x => (object) match.Groups[i++].Value);
            }

            return null;
        }

        #endregion

        #region HTTP Response Manipulation Methods

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void NoCache(this HttpListenerContext context)
        {
            context.Response.AddHeader(Headers.Expires, "Mon, 26 Jul 1997 05:00:00 GMT");
            context.Response.AddHeader(Headers.LastModified,
                DateTime.UtcNow.ToString(Strings.BrowserTimeFormat, Strings.StandardCultureInfo));
            context.Response.AddHeader(Headers.CacheControl, "no-store, no-cache, must-revalidate");
            context.Response.AddHeader(Headers.Pragma, "no-cache");
        }

        /// <summary>
        /// Sets a response static code of 302 and adds a Location header to the response
        /// in order to direct the client to a different URL.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        /// <returns><b>true</b> if the headers were set, otherwise <b>false</b>.</returns>
        public static bool Redirect(this HttpListenerContext context, string location, bool useAbsoluteUrl = true)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetComponents(UriComponents.Scheme | UriComponents.StrongAuthority,
                    UriFormat.Unescaped);
                location = hostPath + location;
            }

            context.Response.StatusCode = 302;
            context.Response.AddHeader("Location", location);

            return true;
        }

        #endregion

        #region JSON and Exception Extensions

        /// <summary>
        /// Outputs a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns>A <c>true</c> value of type ref=JsonResponseAsync"</returns>
        public static bool JsonResponse(this HttpListenerContext context, object data)
            => context.JsonResponseAsync(data).GetAwaiter().GetResult();

        /// <summary>
        /// Outputs async a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns>A <c>true</c> value of type ref=JsonResponseAsync"</returns>
        public static Task<bool> JsonResponseAsync(this HttpListenerContext context, object data)
            => context.JsonResponseAsync(Json.Serialize(data));

        /// <summary>
        /// Outputs a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The JSON.</param>
        /// <returns> A <c>true</c> value of type ref=JsonResponseAsync"</returns>
        public static bool JsonResponse(this HttpListenerContext context, string json)
            => context.JsonResponseAsync(json).GetAwaiter().GetResult();

        /// <summary>
        /// Outputs async a JSON Response given a JSON string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The JSON.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task for writing the output stream</returns>
        public static Task<bool> JsonResponseAsync(
            this HttpListenerContext context,
            string json,
            CancellationToken cancellationToken = default)
        {
            return context.StringResponseAsync(json, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Outputs a HTML Response given a HTML content
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="htmlContent">Content of the HTML.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task for writing the output stream</returns>
        public static Task<bool> HtmlResponseAsync(
            this HttpListenerContext context,
            string htmlContent,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK,
            CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = (int) statusCode;
            return context.StringResponseAsync(htmlContent, Responses.HtmlContentType, cancellationToken);
        }

        /// <summary>
        /// Outputs async a JSON Response given an exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>A <c>true</c> value when the exception is written to the HTTP Response</returns>
        public static bool JsonExceptionResponse(
            this HttpListenerContext context,
            Exception ex,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.InternalServerError)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.JsonResponse(ex);
        }

        /// <summary>
        /// Outputs a JSON Response given an exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>A task for writing the output stream</returns>
        public static Task<bool> JsonExceptionResponseAsync(
            this HttpListenerContext context,
            Exception ex,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.InternalServerError)
        {
            context.Response.StatusCode = (int)statusCode;
            return context.JsonResponseAsync(ex);
        }

        /// <summary>
        /// Outputs async a string response given a string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="content">The content.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>A task for writing the output stream</returns>
        public static async Task<bool> StringResponseAsync(
            this HttpListenerContext context,
            string content,
            string contentType = "application/json",
            CancellationToken cancellationToken = default,
            Encoding encoding = null)
        {
            var buffer = (encoding ?? Encoding.UTF8).GetBytes(content);

            context.Response.ContentType = contentType;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

            return true;
        }

        /// <summary>
        /// Parses the JSON as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T">The type of specified object type</typeparam>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Parses the json as a given type from the request body
        /// </returns>
        public static T ParseJson<T>(this HttpListenerContext context)
            where T : class
        {
            return ParseJson<T>(context.RequestBody());
        }

        /// <summary>
        /// Parses the JSON as a given type from the request body string.
        /// </summary>
        /// <typeparam name="T">The type of specified object type</typeparam>
        /// <param name="requestBody">The request body.</param>
        /// <returns>
        /// A string that represents the json as a given type from the request body string
        /// </returns>
        public static T ParseJson<T>(this string requestBody)
            where T : class
        {
            return requestBody == null ? null : Json.Deserialize<T>(requestBody);
        }

        #endregion

        #region Data Parsing Methods

        /// <summary>
        /// Returns a dictionary of KVPs from Request data
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <returns>A collection that represents KVPs from request data</returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this string requestBody)
            => FormDataParser.ParseAsDictionary(requestBody);

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context">The context to request body as string</param>
        /// <returns>A collection that represents KVPs from request data</returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this HttpListenerContext context)
            => RequestFormDataDictionary(context.RequestBody());

        #endregion

        #region Hashing and Compression Methods

        /// <summary>
        /// Compresses the specified buffer stream using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="method">The method.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Block of bytes of compressed stream</returns>
        public static MemoryStream Compress(
            this Stream buffer,
            CompressionMethod method = CompressionMethod.Gzip,
            CompressionMode mode = CompressionMode.Compress)
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
                            buffer.CopyTo(compressor, 1024);
                            buffer.CopyTo(compressor);

                            // WebSocket use this
                            targetStream.Write(LastByte, 0, 1);
                            targetStream.Position = 0;
                        }
                    }
                    else
                    {
                        using (var compressor = new DeflateStream(buffer, CompressionMode.Decompress))
                        {
                            compressor.CopyTo(targetStream);
                        }
                    }

                    break;
                case CompressionMethod.Gzip:
                    if (mode == CompressionMode.Compress)
                    {
                        using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
                        {
                            buffer.CopyTo(compressor);
                        }
                    }
                    else
                    {
                        using (var compressor = new GZipStream(buffer, CompressionMode.Decompress))
                        {
                            compressor.CopyTo(targetStream);
                        }
                    }

                    break;
                case CompressionMethod.None:
                    buffer.CopyTo(targetStream);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, null);
            }

            return targetStream;
        }

        /// <summary>
        /// Compresses/Decompresses the specified buffer using the compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="method">The method.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>Block of bytes of compressed stream </returns>
        public static byte[] Compress(
            this byte[] buffer, 
            CompressionMethod method = CompressionMethod.Gzip,
            CompressionMode mode = CompressionMode.Compress)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return stream.Compress(method, mode).ToArray();
            }
        }

        #endregion

        internal static string CleanParamId(this string val) => val.ReplaceAll(string.Empty, '{', '}', '?');

        internal static Uri ToUri(this string uriString)
        {
            Uri.TryCreate(
                uriString, uriString.MaybeUri() ? UriKind.Absolute : UriKind.Relative, out var ret);

            return ret;
        }

        internal static bool MaybeUri(this string value)
        {
            var idx = value?.IndexOf(':');

            if (idx.HasValue == false || idx == -1)
                return false;

            return idx < 10 && value.Substring(0, idx.Value).IsPredefinedScheme();
        }

        internal static bool IsPredefinedScheme(this string value)
        {
            if (value == null || value.Length < 2)
                return false;

            var c = value[0];

            switch (c)
            {
                case 'h':
                    return value == "http" || value == "https";
                case 'w':
                    return value == "ws" || value == "wss";
                case 'f':
                    return value == "file" || value == "ftp";
                case 'n':
                    c = value[1];
                    return c == 'e'
                        ? value == "news" || value == "net.pipe" || value == "net.tcp"
                        : value == "nntp";
                default:
                    return (c == 'g' && value == "gopher") || (c == 'm' && value == "mailto");
            }
        }
    }
}
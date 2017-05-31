namespace Unosquare.Labs.EmbedIO
{
    using System.Collections.Generic;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using Swan.Formatters;
    using System.Threading;
    using System.Threading.Tasks;
#if NET46
    using System.Net;
#else
    using Net;
#endif
    
    /// <summary>
    /// Extension methods to help your coding!
    /// </summary>
    public static partial class Extensions
    {
        #region Constants

        private const string UrlEncodedContentType = "application/x-www-form-urlencoded";

        #endregion

        #region Session Management Methods

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this HttpListenerContext context, WebServer server)
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static void DeleteSession(this HttpListenerContext context, WebServer server)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the session object associated to the current context.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static void DeleteSession(this WebServer server, HttpListenerContext context)
        {
            server.SessionModule?.DeleteSession(context);
        }

        /// <summary>
        /// Deletes the given session object.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="session">The session info.</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
#if NET46
        public static SessionInfo GetSession(this System.Net.WebSockets.WebSocketContext context, WebServer server)
#else
        public static SessionInfo GetSession(this Unosquare.Net.WebSocketContext context, WebServer server)
#endif
        {
            return server.SessionModule?.GetSession(context);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
#if NET46
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
        /// <returns></returns>
        public static string RequestPath(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the request path for the specified context case sensitive.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestPathCaseSensitive(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath;
        }

        /// <summary>
        /// Retrieves the Request HTTP Verb (also called Method) of this context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static HttpVerbs RequestVerb(this HttpListenerContext context)
        {
            HttpVerbs verb;
            Enum.TryParse(context.Request.HttpMethod.ToLowerInvariant().Trim(), true, out verb);
            return verb;
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// If the value does not exist it returns null.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string QueryString(this HttpListenerContext context, string key)
        {
            return context.InQueryString(key) ? context.Request.QueryString[key] : null;
        }

        /// <summary>
        /// Determines if a key exists within the Request's query string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static bool InQueryString(this HttpListenerContext context, string key)
        {
            return context.Request.QueryString.AllKeys.Contains(key);
        }

        /// <summary>
        /// Retrieves the specified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static string RequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.HasRequestHeader(headerName) == false ? string.Empty : context.Request.Headers[headerName];
        }

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static bool HasRequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.Request.Headers[headerName] != null;
        }

        /// <summary>
        /// Retrieves the request body as a string.
        /// Note that once this method returns, the underlying input stream cannot be read again as 
        /// it is not rewindable for obvious reasons. This functionality is by design.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
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

        #endregion

        #region HTTP Response Manipulation Methods

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void NoCache(this HttpListenerContext context)
        {
            context.Response.AddHeader(Constants.HeaderExpires, "Mon, 26 Jul 1997 05:00:00 GMT");
            context.Response.AddHeader(Constants.HeaderLastModified,
                DateTime.UtcNow.ToString(Constants.BrowserTimeFormat, Constants.StandardCultureInfo));
            context.Response.AddHeader(Constants.HeaderCacheControl, "no-store, no-cache, must-revalidate");
            context.Response.AddHeader(Constants.HeaderPragma, "no-cache");
        }

        /// <summary>
        /// Sets a response static code of 302 and adds a Location header to the response
        /// in order to direct the client to a different URL
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        public static bool Redirect(this HttpListenerContext context, string location, bool useAbsoluteUrl = true)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetComponents(UriComponents.Scheme | UriComponents.StrongAuthority, UriFormat.Unescaped);
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
            /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, object data)
        {
           return context.JsonResponseAsync(data).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Outputs async a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static Task<bool> JsonResponseAsync(this HttpListenerContext context, object data)
        {
            var jsonFormatting = true;
#if DEBUG
            jsonFormatting = false;
#endif
            return context.JsonResponseAsync(Json.Serialize(data, jsonFormatting));
        }

        /// <summary>
        /// Outputs a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, string json)
        {
            return context.JsonResponseAsync(json).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Outputs async a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The json.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public static async Task<bool> JsonResponseAsync(this HttpListenerContext context, string json, CancellationToken cancellationToken = default(CancellationToken))
        {
            var buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

            return true;
        }

        /// <summary>
        /// Parses the json as a given type from the request body.
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static T ParseJson<T>(this HttpListenerContext context)
            where T : class
        {
            return ParseJson<T>(context.RequestBody());
        }

        /// <summary>
        /// Parses the json as a given type from the request body string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="requestBody">The request body.</param>
        /// <returns></returns>
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
        /// <returns></returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this string requestBody)
        {
            return ParseFormDataAsDictionary(requestBody);
        }

        /// <summary>
        /// Returns dictionary from Request POST data
        /// Please note the underlying input stream is not rewindable.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Dictionary<string, object> RequestFormDataDictionary(this HttpListenerContext context)
        {
            return RequestFormDataDictionary(context.RequestBody());
        }

        /// <summary>
        /// Parses the form data given the request body string.
        /// </summary>
        /// <param name="requestBody">The request body.</param>
        /// <param name="contentTypeHeader">The content type header.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">multipart/form-data Content Type parsing is not yet implemented</exception>
        private static Dictionary<string, object> ParseFormDataAsDictionary(string requestBody,
            string contentTypeHeader = UrlEncodedContentType)
        {
            // TODO: implement multipart/form-data parsing
            // example available here: http://stackoverflow.com/questions/5483851/manually-parse-raw-http-data-with-php

            if (contentTypeHeader.ToLowerInvariant().StartsWith("multipart/form-data"))
                throw new NotImplementedException("multipart/form-data Content Type parsing is not yet implemented");

            // verify there is data to parse
            if (string.IsNullOrWhiteSpace(requestBody)) return null;

            // define a character for KV pairs
            var kvpSeparator = new[] {'='};

            // Create the result object
            var resultDictionary = new Dictionary<string, object>();

            // Split the request body into key-value pair strings
            var keyValuePairStrings = requestBody.Split('&');

            foreach (var kvps in keyValuePairStrings)
            {
                // Skip KVP strings if they are empty
                if (string.IsNullOrWhiteSpace(kvps))
                    continue;

                // Split by the equals char into key values.
                // Some KVPS will have only their key, some will have both key and value
                // Some other might be repeated which really means an array
                var kvpsParts = kvps.Split(kvpSeparator, 2);

                // We don't want empty KVPs
                if (kvpsParts.Length == 0)
                    continue;

                // Decode the key and the value. Discard Special Characters
                var key = System.Net.WebUtility.UrlDecode(kvpsParts[0]);
                if (key.IndexOf("[", StringComparison.OrdinalIgnoreCase) > 0)
                    key = key.Substring(0, key.IndexOf("[", StringComparison.OrdinalIgnoreCase));

                var value = kvpsParts.Length >= 2 ? System.Net.WebUtility.UrlDecode(kvpsParts[1]) : null;

                // If the result already contains the key, then turn the value of that key into a List of strings
                if (resultDictionary.ContainsKey(key))
                {
                    // Check if this key has a List value already
                    var listValue = resultDictionary[key] as List<string>;
                    if (listValue == null)
                    {
                        // if we don't have a list value for this key, then create one and add the existing item
                        var existingValue = resultDictionary[key] as string;
                        resultDictionary[key] = new List<string>();
                        listValue = (List<string>) resultDictionary[key];
                        listValue.Add(existingValue);
                    }

                    // By this time, we are sure listValue exists. Simply add the item
                    listValue.Add(value);
                }
                else
                {
                    // Simply set the key to the parsed value
                    resultDictionary[key] = value;
                }
            }

            return resultDictionary;
        }

        #endregion

        #region Hashing and Compression Methods

        private static readonly byte[] Last = { 0x00 };

        /// <summary>
        /// Compresses the specified buffer stream using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="method">The method.</param>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static MemoryStream Compress(this Stream buffer, CompressionMethod method = CompressionMethod.Gzip,
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
                            targetStream.Write(Last, 0, 1);
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
        /// <returns></returns>
        public static byte[] Compress(this byte[] buffer, CompressionMethod method = CompressionMethod.Gzip, CompressionMode mode = CompressionMode.Compress)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return stream.Compress(method, mode).ToArray();
            }
        }

        #endregion
    }
}
namespace Unosquare.Labs.EmbedIO
{
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.WebSockets;

    /// <summary>
    /// Extension methods to help your coding!
    /// </summary>
    static public class Extensions
    {
        public const string HeaderAcceptEncoding = "Accept-Encoding";
        public const string HeaderContentEncoding = "Content-Encoding";
        public const string HeaderIfModifiedSince = "If-Modified-Since";
        public const string HeaderCacheControl = "Cache-Control";
        public const string HeaderPragma = "Pragma";
        public const string HeaderExpires = "Expires";
        public const string HeaderLastModified = "Last-Modified";
        public const string BrowserTimeFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        static public SessionInfo GetSession(this HttpListenerContext context, WebServer server)
        {
            if (server.SessionModule == null)
                return null;

            return server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        static public SessionInfo GetSession(this WebSocketContext context, WebServer server)
        {
            if (server.SessionModule == null)
                return null;

            return server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        static public SessionInfo GetSession(this WebServer server, HttpListenerContext context)
        {
            if (server.SessionModule == null)
                return null;

            return server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        static public SessionInfo GetSession(this WebServer server, WebSocketContext context)
        {
            if (server.SessionModule == null)
                return null;

            return server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the request path for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        static public string RequestPath(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath.ToLowerInvariant();
        }

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        static public string ExceptionMessage(this Exception ex)
        {
            return ex.ExceptionMessage(string.Empty);
        }

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="priorMessage">The prior message.</param>
        /// <returns></returns>
        static public string ExceptionMessage(this Exception ex, string priorMessage)
        {
            var fullMessage = string.IsNullOrWhiteSpace(priorMessage) ? ex.Message : priorMessage + "\r\n" + ex.Message;
            if (ex.InnerException != null && string.IsNullOrWhiteSpace(ex.InnerException.Message) == false)
                return ExceptionMessage(ex.InnerException, fullMessage);
            else
                return fullMessage;
        }

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        static public void NoCache(this HttpListenerContext context)
        {
            context.Response.AddHeader(HeaderExpires, "Mon, 26 Jul 1997 05:00:00 GMT");
            context.Response.AddHeader(HeaderLastModified, DateTime.UtcNow.ToString(BrowserTimeFormat));
            context.Response.AddHeader(HeaderCacheControl, "no-store, no-cache, must-revalidate");
            context.Response.AddHeader(HeaderPragma, "no-cache");
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        static public string QueryString(this HttpListenerContext context, string key)
        {
            if (context.InQueryString(key))
                return context.Request.QueryString[key];

            return null;
        }

        /// <summary>
        /// Determines if a key exists within the Request's query string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        static public bool InQueryString(this HttpListenerContext context, string key)
        {
            return context.Request.QueryString.AllKeys.Contains(key);
        }

        /// <summary>
        /// Retrieves the Request Verb of this contetext.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        static public HttpVerbs RequestVerb(this HttpListenerContext context)
        {
            var verb = HttpVerbs.Get;
            Enum.TryParse<HttpVerbs>(context.Request.HttpMethod.ToLowerInvariant().Trim(), true, out verb);
            return verb;
        }

        /// <summary>
        /// Redirects the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        static public void Redirect(this HttpListenerContext context, string location, bool useAbsoluteUrl)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetLeftPart(UriPartial.Authority);
                location = hostPath + location;
            }

            context.Response.StatusCode = 302;
            context.Response.AddHeader("Location", location);
        }

        /// <summary>
        /// Prettifies the json.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        static public string PrettifyJson(this string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        /// <summary>
        /// Outputs a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        static public bool JsonResponse(this HttpListenerContext context, object data)
        {
            var jsonFormatting = Formatting.None;
#if DEBUG
            jsonFormatting = Formatting.Indented;
#endif
            var json = JsonConvert.SerializeObject(data, jsonFormatting);
            return context.JsonResponse(json);
        }

        /// <summary>
        /// Outputs a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        static public bool JsonResponse(this HttpListenerContext context, string json)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            return true;
        }

        /// <summary>
        /// Parses the json as a given type from the request body.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        static public T ParseJson<T>(this HttpListenerContext context)
            where T : class
        {
            var body = context.RequestBody();
            if (body == null) return null;

            return JsonConvert.DeserializeObject<T>(body);
        }

        /// <summary>
        /// Retrieves the request body
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        static public string RequestBody(this HttpListenerContext context)
        {
            if (context.Request.HasEntityBody == false)
                return null;

            using (var body = context.Request.InputStream) // here we have data
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, context.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Retrieves the spcified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        static public string RequestHeader(this HttpListenerContext context, string headerName)
        {
            if (context.HasRequestHeader(headerName) == false) return string.Empty;
            return context.Request.Headers[headerName];
        }

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        static public bool HasRequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.Request.Headers[headerName] != null;
        }

        /// <summary>
        /// Compresses the specified buffer using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        static public byte[] Compress(this byte[] buffer)
        {
            byte[] outputBuffer = null;
            using (MemoryStream targetStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
                {
                    compressor.Write(buffer, 0, buffer.Length);
                }
                outputBuffer = targetStream.ToArray();
            }

            return outputBuffer;
        }
    }

}

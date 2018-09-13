namespace Unosquare.Net
{
    using System;
    using Labs.EmbedIO.Constants;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    internal class HttpRequest : HttpBase
    {
        private bool _websocketRequest;
        private bool _websocketRequestSet;
        
        internal HttpRequest(string method, string uri)
          : this(method, uri, HttpVersion.Version11, new NameValueCollection())
        {
            Headers["User-Agent"] = "embedio/2.0";
        }

        private HttpRequest(string method, string uri, Version version, NameValueCollection headers)
          : base(version, headers)
        {
            HttpMethod = method;
            RequestUri = uri;
        }
        
        public CookieCollection Cookies => Headers.GetCookies(false);

        public string HttpMethod { get; }

        public bool IsWebSocketRequest
        {
            get
            {
                if (!_websocketRequestSet)
                {
                    var headers = Headers;
                    _websocketRequest = HttpMethod == "GET" &&
                                        ProtocolVersion > HttpVersion.Version10 &&
                                        headers.Contains("Upgrade", "websocket") &&
                                        headers.Contains("Connection", "Upgrade");

                    _websocketRequestSet = true;
                }

                return _websocketRequest;
            }
        }

        public string RequestUri { get; }
        
        public void SetCookies(CookieCollection cookies)
        {
            if (cookies.Count == 0)
                return;

            var buff = new StringBuilder(64);

            foreach (var cookie in cookies)
            {
                if (!cookie.Expired)
                    buff.AppendFormat("{0}; ", cookie);
            }

            var len = buff.Length;

            if (len > 2)
            {
                buff.Length = len - 2;
                Headers["Cookie"] = buff.ToString();
            }
        }

        public override string ToString()
        {
            var output = new StringBuilder(64);
            output.AppendFormat("{0} {1} HTTP/{2}{3}", HttpMethod, RequestUri, ProtocolVersion, CrLf);

            var headers = Headers;
            foreach (var key in headers.AllKeys)
                output.AppendFormat("{0}: {1}{2}", key, headers[key], CrLf);

            output.Append(CrLf);

            var entity = EntityBody;
            if (entity.Length > 0)
                output.Append(entity);

            return output.ToString();
        }
        
        internal static HttpRequest CreateHandshakeRequest(WebSocket webSocket)
        {
            var ret = CreateWebSocketRequest(webSocket.Url);

            var headers = ret.Headers;

            if (!string.IsNullOrEmpty(webSocket.Origin))
                headers["Origin"] = webSocket.Origin;

            headers["Sec-WebSocket-Key"] = webSocket.WebSocketKey.KeyValue;

            webSocket.IsExtensionsRequested = webSocket.Compression != CompressionMethod.None;

            if (webSocket.IsExtensionsRequested)
                headers["Sec-WebSocket-Extensions"] = CreateExtensions(webSocket.Compression);

            headers["Sec-WebSocket-Version"] = Strings.WebSocketVersion;

            ret.SetCookies(webSocket.CookieCollection);

            return ret;
        }

        internal static HttpRequest Parse(string[] headerParts)
        {
            var requestLine = headerParts[0].Split(new[] { ' ' }, 3);

            if (requestLine.Length != 3)
                throw new ArgumentException($"Invalid request line: {headerParts[0]}");

            return new HttpRequest(requestLine[0], requestLine[1], new Version(requestLine[2].Substring(5)), ParseHeaders(headerParts));
        }
        
        internal Task<HttpResponse> GetResponse(Stream stream)
        {
            var buff = ToByteArray();
            stream.Write(buff, 0, buff.Length);

            return ReadAsync(stream);
        }
        
        // As client
        private static string CreateExtensions(CompressionMethod compression)
        {
            var buff = new StringBuilder(80);

            if (compression != CompressionMethod.None)
            {
                var str = compression.ToExtensionString(
                    "server_no_context_takeover", "client_no_context_takeover");

                buff.AppendFormat("{0}, ", str);
            }

            var len = buff.Length;

            if (len <= 2) return null;

            buff.Length = len - 2;
            return buff.ToString();
        }

        private static HttpRequest CreateWebSocketRequest(Uri uri)
        {
            var req = new HttpRequest("GET", uri.PathAndQuery);
            var headers = req.Headers;

            // Only includes a port number in the Host header value if it's non-default.
            // See: https://tools.ietf.org/html/rfc6455#page-17
            var port = uri.Port;
            var schm = uri.Scheme;
            headers["Host"] = (port == 80 && schm == "ws") || (port == 443 && schm == "wss")
                ? uri.DnsSafeHost
                : uri.Authority;

            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return req;
        }
    }
}
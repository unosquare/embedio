namespace Unosquare.Net
{
    using System;
    using Labs.EmbedIO.Constants;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using HttpHeaders = Labs.EmbedIO.Constants.Headers;

    internal class HttpRequest : HttpBase
    {
        public HttpRequest(string method, string uri)
          : base(HttpVersion.Version11, new NameValueCollection())
        {
            HttpMethod = method;
            RequestUri = uri;

            Headers["User-Agent"] = HttpResponse.ServerVersion;
        }
        
        public CookieCollection Cookies => Headers.GetCookies(false);

        public string HttpMethod { get; }

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
                Headers[HttpHeaders.Cookie] = buff.ToString();
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

            if (EntityBody.Length > 0)
                output.Append(EntityBody);

            return output.ToString();
        }
        
        internal static HttpRequest CreateHandshakeRequest(WebSocket webSocket)
        {
            var ret = CreateWebSocketRequest(webSocket.Url);

            var headers = ret.Headers;

            if (!string.IsNullOrEmpty(webSocket.Origin))
                headers[HttpHeaders.Origin] = webSocket.Origin;

            headers[HttpHeaders.WebSocketKey] = webSocket.WebSocketKey.KeyValue;

            webSocket.IsExtensionsRequested = webSocket.Compression != CompressionMethod.None;

            if (webSocket.IsExtensionsRequested)
                headers[HttpHeaders.WebSocketExtensions] = CreateExtensions(webSocket.Compression);

            headers[HttpHeaders.WebSocketVersion] = Strings.WebSocketVersion;

            ret.SetCookies(webSocket.CookieCollection);

            return ret;
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
            req.Headers["Host"] = (uri.Port == 80 && uri.Scheme == "ws") || (uri.Port == 443 && uri.Scheme == "wss")
                ? uri.DnsSafeHost
                : uri.Authority;

            req.Headers["Upgrade"] = "websocket";
            req.Headers["Connection"] = "Upgrade";

            return req;
        }
    }
}
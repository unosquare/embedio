namespace Unosquare.Net
{
    using System;
    using Labs.EmbedIO;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Text;

    internal class HttpResponse
    {
        internal const string ServerVersion = "embedio/2.0";

        internal HttpResponse(HttpStatusCode code)
          : this((int) code, HttpStatusDescription.Get(code), HttpVersion.Version11, new NameValueCollection())
        {
        }
        
        private HttpResponse(int code, string reason, Version version, NameValueCollection headers)
        {
            ProtocolVersion = version;
            Headers = headers;
            StatusCode = code;
            Reason = reason;
            Headers[HttpHeaderNames.Server] = ServerVersion;
        }
        
        public string Reason { get; }

        public int StatusCode { get; }
        
        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        public void SetCookies(CookieCollection cookies)
        {
            foreach (var cookie in cookies)
                Headers.Add(HttpHeaderNames.SetCookie, cookie.ToString());
        }

        public override string ToString()
        {
            var output = new StringBuilder(64)
                .AppendFormat("HTTP/{0} {1} {2}\r\n", ProtocolVersion, StatusCode, Reason);

            foreach (var key in Headers.AllKeys)
                output.AppendFormat("{0}: {1}\r\n", key, Headers[key]);

            output.Append("\r\n");
            
            return output.ToString();
        }
        
        internal static string GetValue(string nameAndValue)
        {
            var idx = nameAndValue.IndexOf('=');

            return idx < 0 || idx == nameAndValue.Length - 1 ? null : nameAndValue.Substring(idx + 1).Trim().Unquote();
        }

        internal static Encoding GetEncoding(string contentType) => contentType
            .Split(';')
            .Select(p => p.Trim())
            .Where(part => part.StartsWith("charset", StringComparison.OrdinalIgnoreCase))
            .Select(part => Encoding.GetEncoding(GetValue(part)))
            .FirstOrDefault();

        internal static HttpResponse CreateWebSocketResponse()
        {
            var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);

            var headers = res.Headers;
            headers[HttpHeaderNames.Upgrade] = "websocket";
            headers[HttpHeaderNames.Connection] = "Upgrade";

            return res;
        }
    }
}
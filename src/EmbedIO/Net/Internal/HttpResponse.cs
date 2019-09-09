using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace EmbedIO.Net.Internal
{
    internal class HttpResponse
    {
        internal const string ServerVersion = "embedio/3.0";

        internal HttpResponse(HttpStatusCode code)
          : this((int) code, HttpListenerResponseHelper.GetStatusDescription((int)code), HttpVersion.Version11, new NameValueCollection())
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

        public void SetCookies(ICookieCollection cookies)
        {
            foreach (var cookie in cookies)
                Headers.Add("Set-Cookie", cookie.ToString());
        }

        public override string ToString()
        {
            var output = new StringBuilder(64)
                .AppendFormat(CultureInfo.InvariantCulture, "HTTP/{0} {1} {2}\r\n", ProtocolVersion, StatusCode, Reason);

            foreach (var key in Headers.AllKeys)
                output.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", key, Headers[key]);

            output.Append("\r\n");
            
            return output.ToString();
        }

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
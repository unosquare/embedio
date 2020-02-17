using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;

namespace EmbedIO.Net.Internal
{
    internal class WebSocketHandshakeResponse
    {
        private const int HandshakeStatusCode = (int)HttpStatusCode.SwitchingProtocols;

        internal WebSocketHandshakeResponse(IHttpContext context)
        {
            ProtocolVersion = HttpVersion.Version11;
            Headers = context.Response.Headers;
            Headers.Clear(); // Use only headers mentioned in RFC6455 - scrap all the rest.
            StatusCode = HandshakeStatusCode;
            Reason = HttpListenerResponseHelper.GetStatusDescription(HandshakeStatusCode);

            Headers[HttpHeaderNames.Upgrade] = "websocket";
            Headers[HttpHeaderNames.Connection] = "Upgrade";

            foreach (var cookie in context.Request.Cookies)
                Headers.Add("Set-Cookie", cookie.ToString());
        }

        public string Reason { get; }

        public int StatusCode { get; }
        
        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        public override string ToString()
        {
            var output = new StringBuilder(64)
                .AppendFormat(CultureInfo.InvariantCulture, "HTTP/{0} {1} {2}\r\n", ProtocolVersion, StatusCode, Reason);

            foreach (var key in Headers.AllKeys)
                output.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", key, Headers[key]);

            output.Append("\r\n");
            
            return output.ToString();
        }
    }
}
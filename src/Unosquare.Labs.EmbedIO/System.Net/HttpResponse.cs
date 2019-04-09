namespace Unosquare.Net
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Text;

    internal class HttpResponse : HttpBase
    {
        internal const string ServerVersion = "embedio/2.0";

        internal HttpResponse(HttpStatusCode code)
          : this((int) code, HttpListenerResponseHelper.GetStatusDescription((int)code), HttpVersion.Version11, new NameValueCollection())
        {
        }
        
        private HttpResponse(int code, string reason, Version version, NameValueCollection headers)
          : base(version, headers)
        {
            StatusCode = code;
            Reason = reason;
            Headers["Server"] = ServerVersion;
        }
        
        public CookieCollection Cookies => Headers.GetCookies(true);

        public bool IsRedirect => StatusCode == 301 || StatusCode == 302;

        public bool IsUnauthorized => StatusCode == 401;

        public bool IsWebSocketResponse => ProtocolVersion > HttpVersion.Version10 &&
                                           StatusCode == 101 &&
                                           Headers.Contains("Upgrade", "websocket") &&
                                           Headers.Contains("Connection", "Upgrade");

        public string Reason { get; }

        public int StatusCode { get; }
        
        public void SetCookies(CookieCollection cookies)
        {
            foreach (var cookie in cookies)
                Headers.Add("Set-Cookie", cookie.ToString());
        }

        public override string ToString()
        {
            var output = new StringBuilder(64)
                .AppendFormat("HTTP/{0} {1} {2}{3}\r\n", ProtocolVersion, StatusCode, Reason);

            foreach (var key in Headers.AllKeys)
                output.AppendFormat("{0}: {1}{2}\r\n", key, Headers[key]);

            output.Append("\r\n");
            
            return output.ToString();
        }

        internal static HttpResponse CreateWebSocketResponse()
        {
            var res = new HttpResponse(HttpStatusCode.SwitchingProtocols);

            var headers = res.Headers;
            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return res;
        }

        internal static HttpResponse Parse(string[] headerParts)
        {
            var statusLine = headerParts[0].Split(new[] { ' ' }, 3);

            if (statusLine.Length != 3)
                throw new ArgumentException($"Invalid status line: {headerParts[0]}");

            return new HttpResponse(int.Parse(statusLine[1]), statusLine[2], new Version(statusLine[0].Substring(5)), ParseHeaders(headerParts));
        }
    }
}
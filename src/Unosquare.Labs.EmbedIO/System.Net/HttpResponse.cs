﻿namespace Unosquare.Net
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Text;

    internal class HttpResponse
    {
        internal const string ServerVersion = "embedio/2.0";

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
            Headers["Server"] = ServerVersion;
        }
        
        public CookieCollection Cookies => Headers.GetCookies(true);
        
        public string Reason { get; }

        public int StatusCode { get; }
        
        public NameValueCollection Headers { get; }

        public Version ProtocolVersion { get; }

        public void SetCookies(CookieCollection cookies)
        {
            foreach (var cookie in cookies)
                Headers.Add("Set-Cookie", cookie.ToString());
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

        protected static NameValueCollection ParseHeaders(string[] headerParts)
        {
            var headers = new NameValueCollection();

            for (var i = 1; i < headerParts.Length; i++)
            {
                var parts = headerParts[i].Split(':');

                headers[parts[0]] = parts[1];
            }

            return headers;
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
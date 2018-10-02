namespace Unosquare.Net
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using HttpHeaders = Labs.EmbedIO.Constants.Headers;

    internal class WebHeaderCollection
        : NameValueCollection
    {
        private static readonly Dictionary<string, HttpHeaderInfo> Headers = new Dictionary<string, HttpHeaderInfo>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Accept",
                        new HttpHeaderInfo(
                            "Accept",
                            HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptCharset",
                        new HttpHeaderInfo(
                            "Accept-Charset",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptEncoding",
                        new HttpHeaderInfo(
                            HttpHeaders.AcceptEncoding,
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptLanguage",
                        new HttpHeaderInfo(
                            "Accept-Language",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "AcceptRanges",
                        new HttpHeaderInfo(
                            HttpHeaders.AcceptRanges,
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Age",
                        new HttpHeaderInfo(
                            "Age",
                            HttpHeaderType.Response)
                    },
                    {
                        "Allow",
                        new HttpHeaderInfo(
                            "Allow",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Authorization",
                        new HttpHeaderInfo(
                            "Authorization",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "CacheControl",
                        new HttpHeaderInfo(
                            HttpHeaders.CacheControl,
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Connection",
                        new HttpHeaderInfo(
                            "Connection",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "ContentEncoding",
                        new HttpHeaderInfo(
                            HttpHeaders.ContentEncoding,
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "ContentLanguage",
                        new HttpHeaderInfo(
                            "Content-Language",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "ContentLength",
                        new HttpHeaderInfo(
                            "Content-Length",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "ContentLocation",
                        new HttpHeaderInfo(
                            "Content-Location",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ContentMd5",
                        new HttpHeaderInfo(
                            "Content-MD5",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ContentRange",
                        new HttpHeaderInfo(
                            "Content-Range",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ContentType",
                        new HttpHeaderInfo(
                            "Content-Type",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        HttpHeaders.Cookie,
                        new HttpHeaderInfo(
                            HttpHeaders.Cookie,
                            HttpHeaderType.Request)
                    },
                    {
                        "Cookie2",
                        new HttpHeaderInfo(
                            "Cookie2",
                            HttpHeaderType.Request)
                    },
                    {
                        "Date",
                        new HttpHeaderInfo(
                            "Date",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "Expect",
                        new HttpHeaderInfo(
                            "Expect",
                            HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        HttpHeaders.Expires,
                        new HttpHeaderInfo(
                            HttpHeaders.Expires,
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        HttpHeaders.ETag,
                        new HttpHeaderInfo(
                            HttpHeaders.ETag,
                            HttpHeaderType.Response)
                    },
                    {
                        "From",
                        new HttpHeaderInfo(
                            "From",
                            HttpHeaderType.Request)
                    },
                    {
                        "Host",
                        new HttpHeaderInfo(
                            "Host",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "IfMatch",
                        new HttpHeaderInfo(
                            "If-Match",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "IfModifiedSince",
                        new HttpHeaderInfo(
                            HttpHeaders.IfModifiedSince,
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "IfNoneMatch",
                        new HttpHeaderInfo(
                            "If-None-Match",
                            HttpHeaderType.Request | HttpHeaderType.MultiValue)
                    },
                    {
                        "IfRange",
                        new HttpHeaderInfo(
                            "If-Range",
                            HttpHeaderType.Request)
                    },
                    {
                        "IfUnmodifiedSince",
                        new HttpHeaderInfo(
                            "If-Unmodified-Since",
                            HttpHeaderType.Request)
                    },
                    {
                        "KeepAlive",
                        new HttpHeaderInfo(
                            "Keep-Alive",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "LastModified",
                        new HttpHeaderInfo(
                            HttpHeaders.LastModified,
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "Location",
                        new HttpHeaderInfo(
                            "Location",
                            HttpHeaderType.Response)
                    },
                    {
                        "MaxForwards",
                        new HttpHeaderInfo(
                            "Max-Forwards",
                            HttpHeaderType.Request)
                    },
                    {
                        HttpHeaders.Pragma,
                        new HttpHeaderInfo(
                            HttpHeaders.Pragma,
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "ProxyAuthenticate",
                        new HttpHeaderInfo(
                            "Proxy-Authenticate",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "ProxyAuthorization",
                        new HttpHeaderInfo(
                            "Proxy-Authorization",
                            HttpHeaderType.Request)
                    },
                    {
                        "ProxyConnection",
                        new HttpHeaderInfo(
                            "Proxy-Connection",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "Public",
                        new HttpHeaderInfo(
                            "Public",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Range",
                        new HttpHeaderInfo(
                            "Range",
                            HttpHeaderType.Request | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "Referer",
                        new HttpHeaderInfo(
                            "Referer",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "RetryAfter",
                        new HttpHeaderInfo(
                            "Retry-After",
                            HttpHeaderType.Response)
                    },
                    {
                        "SecWebSocketAccept",
                        new HttpHeaderInfo(
                            HttpHeaders.WebSocketAccept,
                            HttpHeaderType.Response | HttpHeaderType.Restricted)
                    },
                    {
                        "SecWebSocketExtensions",
                        new HttpHeaderInfo(
                            HttpHeaders.WebSocketExtensions,
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValueInRequest)
                    },
                    {
                        "SecWebSocketKey",
                        new HttpHeaderInfo(
                            HttpHeaders.WebSocketKey,
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "SecWebSocketProtocol",
                        new HttpHeaderInfo(
                            HttpHeaders.WebSocketProtocol,
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValueInRequest)
                    },
                    {
                        "SecWebSocketVersion",
                        new HttpHeaderInfo(
                            HttpHeaders.WebSocketVersion,
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValueInResponse)
                    },
                    {
                        "Server",
                        new HttpHeaderInfo(
                            "Server",
                            HttpHeaderType.Response)
                    },
                    {
                        "SetCookie",
                        new HttpHeaderInfo(
                            "Set-Cookie",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "SetCookie2",
                        new HttpHeaderInfo(
                            "Set-Cookie2",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Te",
                        new HttpHeaderInfo(
                            "TE",
                            HttpHeaderType.Request)
                    },
                    {
                        "Trailer",
                        new HttpHeaderInfo(
                            "Trailer",
                            HttpHeaderType.Request | HttpHeaderType.Response)
                    },
                    {
                        "TransferEncoding",
                        new HttpHeaderInfo(
                            "Transfer-Encoding",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.Restricted | HttpHeaderType.MultiValue)
                    },
                    {
                        "Translate",
                        new HttpHeaderInfo(
                            "Translate",
                            HttpHeaderType.Request)
                    },
                    {
                        "Upgrade",
                        new HttpHeaderInfo(
                            "Upgrade",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "UserAgent",
                        new HttpHeaderInfo(
                            "User-Agent",
                            HttpHeaderType.Request | HttpHeaderType.Restricted)
                    },
                    {
                        "Vary",
                        new HttpHeaderInfo(
                            "Vary",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Via",
                        new HttpHeaderInfo(
                            "Via",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "Warning",
                        new HttpHeaderInfo(
                            "Warning",
                            HttpHeaderType.Request | HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                    {
                        "WwwAuthenticate",
                        new HttpHeaderInfo(
                            "WWW-Authenticate",
                            HttpHeaderType.Response | HttpHeaderType.MultiValue)
                    },
                };

        internal HttpHeaderType State { get; private set; }

        public override string ToString()
        {
            var buff = new StringBuilder();

            foreach (string key in Keys)
                buff.AppendFormat("{0}: {1}\r\n", key, Get(key));

            return buff.Append("\r\n").ToString();
        }

        internal static bool IsHeaderValue(string value)
        {
            var len = value.Length;
            for (var i = 0; i < len; i++)
            {
                var c = value[i];
                if (c < 0x20 && !"\r\n\t".Contains(c))
                    return false;

                if (c == 0x7f)
                    return false;

                if (c == '\n' && ++i < len)
                {
                    c = value[i];
                    if (!" \t".Contains(c))
                        return false;
                }
            }

            return true;
        }

        private static HttpHeaderType CheckHeaderType(string name)
        {
            var info = GetHeaderInfo(name);

            return info == null
                ? HttpHeaderType.Unspecified
                : info.IsRequest && !info.IsResponse
                    ? HttpHeaderType.Request
                    : !info.IsRequest && info.IsResponse
                        ? HttpHeaderType.Response
                        : HttpHeaderType.Unspecified;
        }

        private static string CheckValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Trim();
            if (value.Length > 65535)
                throw new ArgumentOutOfRangeException(nameof(value), "Greater than 65,535 characters.");

            if (!IsHeaderValue(value))
                throw new ArgumentException("Contains invalid characters.", nameof(value));

            return value;
        }

        private static HttpHeaderInfo GetHeaderInfo(string name)
            => Headers.Values.FirstOrDefault(info => info.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public override void Add(string name, string value)
        {
            var type = CheckHeaderType(name);

            if (type == HttpHeaderType.Unspecified)
            {
                base.Add(name, CheckValue(value));
            }
            else
            {
                CheckState(type == HttpHeaderType.Response);

                base.Add(name, CheckValue(value));

                State = type == HttpHeaderType.Response ? HttpHeaderType.Response : HttpHeaderType.Request;
            }
        }

        private void CheckState(bool response)
        {
            if (State == HttpHeaderType.Unspecified)
                return;

            if (response && State == HttpHeaderType.Request)
            {
                throw new InvalidOperationException(
                      "This collection has already been used to store the request headers.");
            }

            if (!response && State == HttpHeaderType.Response)
            {
                throw new InvalidOperationException(
                      "This collection has already been used to store the response headers.");
            }
        }
    }

    [Flags]
    internal enum HttpHeaderType
    {
        /// <summary>
        /// The unspecified
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// The request
        /// </summary>
        Request = 1,

        /// <summary>
        /// The response
        /// </summary>
        Response = 1 << 1,

        /// <summary>
        /// The restricted
        /// </summary>
        Restricted = 1 << 2,

        /// <summary>
        /// The multi-value
        /// </summary>
        MultiValue = 1 << 3,

        /// <summary>
        /// The multi-value in request
        /// </summary>
        MultiValueInRequest = 1 << 4,

        /// <summary>
        /// The multi-value in response
        /// </summary>
        MultiValueInResponse = 1 << 5,
    }

    internal class HttpHeaderInfo
    {
        internal HttpHeaderInfo(string name, HttpHeaderType type)
        {
            Name = name;
            Type = type;
        }

        public bool IsRequest => (Type & HttpHeaderType.Request) == HttpHeaderType.Request;

        public bool IsResponse => (Type & HttpHeaderType.Response) == HttpHeaderType.Response;

        public string Name { get; }

        public HttpHeaderType Type { get; }
    }
}